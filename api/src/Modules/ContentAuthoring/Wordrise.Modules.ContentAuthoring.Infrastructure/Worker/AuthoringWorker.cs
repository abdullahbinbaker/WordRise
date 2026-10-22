using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Worker;

/// <summary>
/// Processes every waiting draft, each in its own unit of work, so one draft failing (or the
/// database object it used going bad) never spoils the next.
/// </summary>
internal sealed class DraftQueueRunner(
    IServiceScopeFactory scopes,
    IOptions<AuthoringOptions> options,
    ILogger<DraftQueueRunner> logger) : IDraftQueueRunner
{
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> waiting;
        await using (var scope = scopes.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<IDraftProcessor>();
            waiting = await processor.GetQueuedIdsAsync(options.Value.Worker.MaxDraftsPerPoll, cancellationToken);
        }

        var processed = 0;
        foreach (var draftId in waiting)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var scope = scopes.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IDraftProcessor>();
            if (await processor.ProcessAsync(draftId, cancellationToken))
            {
                processed++;
            }
        }

        if (processed > 0)
        {
            logger.LogInformation("Authoring processed {Count} draft(s)", processed);
        }

        return processed;
    }

    public async Task<int> RecoverInterruptedAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IDraftProcessor>().RecoverInterruptedAsync(cancellationToken);
    }
}

/// <summary>Checks the queue on a timer and works through it: lessons get written while nobody is looking.</summary>
internal sealed class AuthoringWorker(
    IDraftQueueRunner runner,
    IOptions<AuthoringOptions> options,
    ILogger<AuthoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Worker.Enabled)
        {
            logger.LogInformation("The authoring worker is switched off (Authoring:Worker:Enabled)");
            return;
        }

        if (settings.Provider == AuthoringProvider.Claude && string.IsNullOrWhiteSpace(settings.Claude.ApiKey))
        {
            logger.LogWarning("Authoring:Provider is Claude but Authoring:Claude:ApiKey is not set: queued drafts will fail until it is");
        }

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.Worker.PollIntervalSeconds));
            var recovered = false;
            do
            {
                try
                {
                    // A restart can cut a draft off halfway; put it back so it is not stuck "in progress" forever.
                    // Retried each round until it works: a database that is not up yet must not stop the app.
                    if (!recovered)
                    {
                        await runner.RecoverInterruptedAsync(stoppingToken);
                        recovered = true;
                    }

                    await runner.RunOnceAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "The authoring worker could not process the queue; it will try again");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is shutting down.
        }
    }
}

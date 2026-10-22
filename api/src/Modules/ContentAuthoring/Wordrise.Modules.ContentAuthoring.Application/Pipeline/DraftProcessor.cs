using Microsoft.Extensions.Logging;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Application.Pipeline;

/// <summary>Works on one draft at a time. Scoped: one instance per unit of work.</summary>
public interface IDraftProcessor
{
    /// <summary>Ids of drafts waiting for the writer, oldest first.</summary>
    Task<IReadOnlyList<Guid>> GetQueuedIdsAsync(int take, CancellationToken cancellationToken);

    /// <summary>Runs the write-and-review loop on a queued draft.</summary>
    /// <returns>False when the draft was not waiting any more (someone else took it).</returns>
    Task<bool> ProcessAsync(Guid draftId, CancellationToken cancellationToken);

    /// <summary>Puts drafts whose run was cut short (a restart) back in the queue.</summary>
    Task<int> RecoverInterruptedAsync(CancellationToken cancellationToken);
}

/// <summary>Processes every waiting draft, each in its own unit of work. What the background worker runs.</summary>
public interface IDraftQueueRunner
{
    /// <returns>How many drafts were processed.</returns>
    Task<int> RunOnceAsync(CancellationToken cancellationToken);

    Task<int> RecoverInterruptedAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The authoring pipeline for one draft: the writer writes, free code checks look at the shape,
/// the AI reviewer judges what passes them, and the writer fixes what fails, up to the allowed
/// number of attempts. Every attempt is saved, so a crash loses nothing but the attempt in flight.
/// </summary>
public sealed class DraftProcessor(
    IDraftRepository drafts,
    IUnitOfWork unitOfWork,
    ILessonWriter writer,
    ILessonReviewer reviewer,
    IKnownVocabulary vocabulary,
    IAuthoringSettings settings,
    TimeProvider clock,
    ILogger<DraftProcessor> logger) : IDraftProcessor
{
    private const int MaxErrorLength = 500;

    public Task<IReadOnlyList<Guid>> GetQueuedIdsAsync(int take, CancellationToken cancellationToken) =>
        drafts.GetQueuedIdsAsync(take, cancellationToken);

    public async Task<bool> ProcessAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await drafts.GetAsync(draftId, cancellationToken);
        if (draft is not { Status: DraftStatus.Queued })
        {
            return false;
        }

        // Claim the draft before the slow work, so nobody else starts the same one.
        draft.StartRun(Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await RunCyclesAsync(draft, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The app is shutting down: the draft stays in progress and is requeued at the next start.
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Authoring failed for draft {DraftId}", draftId);
            draft.Fail(Describe(exception), Now);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        return true;
    }

    public async Task<int> RecoverInterruptedAsync(CancellationToken cancellationToken)
    {
        var interrupted = await drafts.GetInProgressAsync(cancellationToken);
        foreach (var draft in interrupted)
        {
            draft.Requeue(Now);
        }

        if (interrupted.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("{Count} interrupted draft(s) put back in the queue", interrupted.Count);
        }

        return interrupted.Count;
    }

    private async Task RunCyclesAsync(LessonDraft draft, CancellationToken cancellationToken)
    {
        var context = new AuthoringContext(
            draft.Brief,
            await vocabulary.GetTaughtWordsAsync(draft.Brief.TargetLanguage, cancellationToken),
            [.. draft.Feedback.Select(f => f.Text)]);

        for (var cycle = 1; cycle <= settings.MaxCycles; cycle++)
        {
            // A lesson sent back by a person is revised, not started over.
            var latest = draft.LatestRevision;
            var content = latest is null
                ? await writer.WriteAsync(context, cancellationToken)
                : await writer.ReviseAsync(context, latest.Content, latest.Review, cancellationToken);

            // Free checks first: a lesson that is not even well formed never reaches the paid reviewer.
            var problems = LessonContentValidator.Validate(draft.Brief, content);
            var review = problems.Count > 0
                ? ReviewReport.FromStructuralProblems(problems)
                : await reviewer.ReviewAsync(context, content, cancellationToken);

            draft.AddRevision(content, review, Now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Draft {DraftId} cycle {Cycle}/{Max}: {Source} review {Outcome}",
                draft.Id, cycle, settings.MaxCycles, review.Source, review.Passed ? "passed" : "failed");

            if (review.Passed)
            {
                draft.ApproveByAi(Now);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }
        }

        draft.FlagNeedsAttention(Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private static string Describe(Exception exception)
    {
        var text = $"{exception.GetType().Name}: {exception.Message}";
        return text.Length <= MaxErrorLength ? text : text[..MaxErrorLength];
    }
}

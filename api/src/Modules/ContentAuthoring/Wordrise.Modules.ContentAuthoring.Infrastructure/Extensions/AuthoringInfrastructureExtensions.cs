using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Contexts;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Claude;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Learning;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Sample;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Queries;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Repositories;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Services;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Worker;
using Wordrise.Shared.Infrastructure.Database;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Extensions;

public static class AuthoringInfrastructureExtensions
{
    public static IServiceCollection AddAuthoringInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // Out-of-range values stop the app at startup instead of failing during a night's run.
        services
            .AddOptions<AuthoringOptions>()
            .Bind(configuration.GetSection(AuthoringOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AuthoringOptions>, AuthoringOptionsValidator>());

        services.AddDbContext<AuthoringDbContext>((provider, options) =>
            options.UseWordriseDatabase(
                provider.GetRequiredService<IOptions<DatabaseOptions>>().Value,
                AuthoringDbContext.Schema));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AuthoringDbContext>());
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IAuthoringQueries, AuthoringQueries>();
        services.AddSingleton<IAuthoringSettings, AuthoringSettings>();

        // The only doors to the Learning module.
        services.AddScoped<ILessonPublisher, LearningLessonPublisher>();
        services.AddScoped<IKnownVocabulary, LearningKnownVocabulary>();

        // Who writes and reviews is a setting: Claude in production, built-in samples in development.
        services.AddSingleton<ClaudeClientProvider>();
        services.AddSingleton<ClaudeJsonClient>();
        services.AddScoped<ILessonWriter>(provider => UsesClaude(provider)
            ? ActivatorUtilities.CreateInstance<ClaudeLessonWriter>(provider)
            : new SampleLessonWriter());
        services.AddScoped<ILessonReviewer>(provider => UsesClaude(provider)
            ? ActivatorUtilities.CreateInstance<ClaudeLessonReviewer>(provider)
            : new SampleLessonReviewer());

        services.AddScoped<IDraftProcessor, DraftProcessor>();
        services.AddSingleton<IDraftQueueRunner, DraftQueueRunner>();
        services.AddHostedService<AuthoringWorker>();

        return services;
    }

    /// <summary>Creates or migrates the schema.</summary>
    public static async Task InitializeAuthoringDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthoringDbContext>();
        var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        await db.PrepareAsync(databaseOptions, cancellationToken);
    }

    private static bool UsesClaude(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<AuthoringOptions>>().Value.Provider == AuthoringProvider.Claude;
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wordrise.Modules.Learning.Api.Services;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Contracts;
using Wordrise.Modules.Learning.Infrastructure.Extensions;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api;

/// <summary>
/// Daily lessons, the word commitment and spaced-repetition reviews. Built in layers:
/// Domain (rules) ← Application (commands and queries) ← Infrastructure (database) ← Api (controllers).
/// </summary>
public sealed class LearningModuleStartup : ModuleStartup
{
    public override string Name => "Learning";

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLearningInfrastructure(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentLearner, HttpCurrentLearner>();
        services.AddScoped<ILessonPublisher, LessonPublisherAdapter>();
        services.AddScoped<IPublishedVocabulary, PublishedVocabularyAdapter>();
        services.AddModuleControllers(typeof(LearningModuleStartup).Assembly);
    }

    public override Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.InitializeLearningDatabaseAsync(cancellationToken);
}

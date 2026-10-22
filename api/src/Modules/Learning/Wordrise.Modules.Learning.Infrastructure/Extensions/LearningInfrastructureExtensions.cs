using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Infrastructure.Contexts;
using Wordrise.Modules.Learning.Infrastructure.Queries;
using Wordrise.Modules.Learning.Infrastructure.Repositories;
using Wordrise.Modules.Learning.Infrastructure.Seeds;
using Wordrise.Modules.Learning.Infrastructure.Services;
using Wordrise.Shared.Infrastructure.Database;

namespace Wordrise.Modules.Learning.Infrastructure.Extensions;

public static class LearningInfrastructureExtensions
{
    public static IServiceCollection AddLearningInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        // A bad value stops the app at startup instead of failing on a learner's request.
        services
            .AddOptions<LearningOptions>()
            .Bind(configuration.GetSection(LearningOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => o.Reviews.MaxPerSession is >= ReviewOptions.MinPerSession and <= ReviewOptions.MaxPerSessionLimit,
                $"Learning:Reviews:MaxPerSession must be {ReviewOptions.MinPerSession} to {ReviewOptions.MaxPerSessionLimit}.")
            .Validate(
                o => o.Reviews.Backlog.Limit is >= ReviewBacklogOptions.MinLimit and <= ReviewBacklogOptions.MaxLimit,
                $"Learning:Reviews:Backlog:Limit must be {ReviewBacklogOptions.MinLimit} to {ReviewBacklogOptions.MaxLimit}.")
            .ValidateOnStart();

        services.AddDbContext<LearningDbContext>((provider, options) =>
            options.UseWordriseDatabase(
                provider.GetRequiredService<IOptions<DatabaseOptions>>().Value,
                LearningDbContext.Schema));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LearningDbContext>());
        services.AddScoped<ILearnerRepository, LearnerRepository>();
        services.AddScoped<IWordProgressRepository, WordProgressRepository>();
        services.AddScoped<ILessonRepository, LessonRepository>();
        services.AddScoped<ILearningQueries, LearningQueries>();
        services.AddSingleton<ILearningCalendar, LearningCalendar>();
        services.AddSingleton<ILearningSettings, LearningSettings>();

        return services;
    }

    /// <summary>Creates or migrates the schema, then adds the sample lessons to an empty course.</summary>
    public static async Task InitializeLearningDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LearningDbContext>();
        var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        await db.PrepareAsync(databaseOptions, cancellationToken);

        // Without migrations applied there are no tables to seed yet.
        if (db.Database.IsNpgsql() && !databaseOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        if (!await db.Lessons.AnyAsync(cancellationToken))
        {
            db.Lessons.AddRange(SampleLessons.All);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

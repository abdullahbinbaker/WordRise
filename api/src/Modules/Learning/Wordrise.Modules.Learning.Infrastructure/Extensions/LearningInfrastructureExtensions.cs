using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Lessons;
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

    /// <summary>Creates or migrates the schema, then adds the sample lesson to an empty course and the foundations course.</summary>
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

        // The foundations course is part of the product, so it is added to any database that lacks it.
        if (!await db.Lessons.AnyAsync(l => l.Track == LessonTrack.Foundations, cancellationToken))
        {
            db.Lessons.AddRange(FoundationLessons.All);
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            await RenumberFoundationsAsync(db, cancellationToken);
        }
    }

    /// <summary>
    /// The course order is letters, numbers, greetings, then the rest. A database seeded with an older order
    /// is renumbered in place (through temporary numbers, because a number is unique within its course).
    /// </summary>
    private static async Task RenumberFoundationsAsync(LearningDbContext db, CancellationToken cancellationToken)
    {
        var wanted = FoundationLessons.All.ToDictionary(l => l.Id, l => l.Number);
        var current = await db.Lessons
            .Where(l => l.Track == LessonTrack.Foundations)
            .Select(l => new { l.Id, l.Number })
            .ToListAsync(cancellationToken);
        var stale = current.Where(l => wanted.TryGetValue(l.Id, out var number) && number != l.Number).ToList();

        foreach (var lesson in stale)
        {
            var temporary = -lesson.Number;
            await db.Lessons.Where(l => l.Id == lesson.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Number, temporary), cancellationToken);
        }

        foreach (var lesson in stale)
        {
            var number = wanted[lesson.Id];
            await db.Lessons.Where(l => l.Id == lesson.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Number, number), cancellationToken);
        }
    }
}

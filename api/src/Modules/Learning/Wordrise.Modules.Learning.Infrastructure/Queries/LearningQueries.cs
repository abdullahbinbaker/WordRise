using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Application.Features.Goal;
using Wordrise.Modules.Learning.Application.Features.Lessons;
using Wordrise.Modules.Learning.Application.Features.Reviews;
using Wordrise.Modules.Learning.Application.Features.Today;
using Wordrise.Modules.Learning.Application.Features.Words;
using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Modules.Learning.Infrastructure.Contexts;

namespace Wordrise.Modules.Learning.Infrastructure.Queries;

/// <summary>Read side: no tracking, results shaped for the screens.</summary>
internal sealed class LearningQueries(LearningDbContext db, ILearningSettings settings) : ILearningQueries
{
    public async Task<DailyGoalDto> GetDailyGoalAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var goal = await db.Learners
            .AsNoTracking()
            .Where(l => l.Id == learnerId)
            .Select(l => (int?)l.DailyGoal)
            .SingleOrDefaultAsync(cancellationToken);
        return new DailyGoalDto(goal ?? Learner.DefaultDailyGoal, Learner.AllowedDailyGoals);
    }

    public async Task<TodayDto> GetTodayAsync(Guid learnerId, DateOnly today, CancellationToken cancellationToken)
    {
        var learner = await db.Learners.AsNoTracking().SingleOrDefaultAsync(l => l.Id == learnerId, cancellationToken);
        HashSet<Guid> completed = [.. learner?.CompletedLessons.Select(c => c.LessonId) ?? []];

        var lessons = await db.Lessons.AsNoTracking().OrderBy(l => l.Number).ToListAsync(cancellationToken);
        var next = lessons.FirstOrDefault(l => !completed.Contains(l.Id));

        var dueCount = settings.ReviewsEnabled
            ? await db.WordProgress.CountAsync(w => w.LearnerId == learnerId && w.DueOn <= today, cancellationToken)
            : 0;
        var learnedCount = await db.WordProgress.CountAsync(w => w.LearnerId == learnerId, cancellationToken);

        return new TodayDto(
            DailyGoal: learner?.DailyGoal ?? Learner.DefaultDailyGoal,
            Streak: learner?.StreakOn(today) ?? 0,
            ReviewsEnabled: settings.ReviewsEnabled,
            DueReviewCount: dueCount,
            LearnedWordsCount: learnedCount,
            CompletedLessonToday: learner?.CompletedAnyLessonOn(today) ?? false,
            NewLessonsPaused: settings.ReviewBacklog.PausesNewLessons(dueCount),
            ReviewBacklogLimit: settings.ReviewBacklog.Limit,
            NextLesson: next is null
                ? null
                : new NextLessonDto(next.Id, next.Number, next.Title, next.Scenario, next.GrammarPoint, next.Content.Words.Count));
    }

    public async Task<LessonDto?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons.AsNoTracking().SingleOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
        return lesson is null ? null : ToDto(lesson);
    }

    public async Task<IReadOnlyList<string>> GetTaughtWordsAsync(string targetLanguage, CancellationToken cancellationToken)
    {
        // The words live inside each lesson's JSON body; a course has tens of lessons, so they are flattened here.
        var lessons = await db.Lessons.AsNoTracking().Where(l => l.TargetLanguage == targetLanguage).OrderBy(l => l.Number).ToListAsync(cancellationToken);
        return
        [
            .. lessons
                .SelectMany(l => l.Content.Words)
                .Select(w => w.Target)
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }

    public async Task<IReadOnlyList<DueReviewDto>> GetDueReviewsAsync(
        Guid learnerId,
        DateOnly today,
        int take,
        CancellationToken cancellationToken) =>
        await db.WordProgress
            .AsNoTracking()
            .Where(w => w.LearnerId == learnerId && w.DueOn <= today)
            .OrderBy(w => w.DueOn)
            .Take(take)
            .Select(w => new DueReviewDto(w.WordId, w.Target, w.Native))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MyWordDto>> ListWordsAsync(
        Guid learnerId,
        WordFilter filter,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var words = await db.WordProgress
            .AsNoTracking()
            .Where(w => w.LearnerId == learnerId)
            .OrderBy(w => w.Target)
            .ToListAsync(cancellationToken);

        // Status is a domain rule; a learner has hundreds of words at most, so it is applied in memory.
        return
        [
            .. words
                .Select(w => new MyWordDto(w.WordId, w.Target, w.Native, w.StatusOn(today), w.DueOn))
                .Where(w => filter switch
                {
                    WordFilter.Due => w.Status == WordStatus.Due,
                    WordFilter.Mastered => w.Status == WordStatus.Mastered,
                    _ => true,
                }),
        ];
    }

    private static LessonDto ToDto(Lesson lesson)
    {
        var content = lesson.Content;
        return new LessonDto(
            lesson.Id,
            lesson.Number,
            lesson.TargetLanguage,
            lesson.NativeLanguage,
            lesson.Title,
            lesson.Scenario,
            lesson.GrammarPoint,
            [.. content.Words.Select(w => new LessonWordDto(w.Id, w.Target, w.Native, w.Pronunciation, w.Example, w.ExampleNative))],
            [.. content.Dialogue.Select(d => new DialogueLineDto(d.Speaker, d.IsLearner, d.Target, d.Native))],
            content.ChatPrompt,
            [.. content.ChatOptions.Select(o => new AnswerOptionDto(o.Text, o.IsCorrect))],
            new GrammarDto(
                content.Grammar.Title,
                content.Grammar.Meaning,
                content.Grammar.Explanation,
                content.Grammar.Pattern,
                [.. content.Grammar.Examples.Select(e => new GrammarExampleDto(e.Target, e.Native))],
                content.Grammar.Note),
            new ListeningDto(
                content.ListeningSentence,
                [.. content.ListeningOptions.Select(o => new AnswerOptionDto(o.Text, o.IsCorrect))]));
    }
}

namespace Wordrise.Modules.Learning.Application.Features.Today;

/// <summary>The home screen: what to study today and how the learner is doing.</summary>
/// <param name="ReviewsEnabled">False: skip the review step, lessons start with the new words.</param>
/// <param name="NewLessonsPaused">
/// True when more than <paramref name="ReviewBacklogLimit"/> words are due: today is reviews only.
/// </param>
/// <param name="ReviewBacklogLimit">Null when the backlog rule is switched off.</param>
public sealed record TodayDto(
    int DailyGoal,
    int Streak,
    bool ReviewsEnabled,
    int DueReviewCount,
    int LearnedWordsCount,
    bool CompletedLessonToday,
    bool NewLessonsPaused,
    int? ReviewBacklogLimit,
    NextLessonDto? NextLesson);

/// <summary>Null on <see cref="TodayDto"/> once every published lesson is completed.</summary>
public sealed record NextLessonDto(
    Guid Id,
    int Number,
    string Title,
    string Scenario,
    string GrammarPoint,
    int NewWordsCount);

using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Domain.Learners;

/// <summary>Someone studying English. Shares its id with their account.</summary>
public sealed class Learner
{
    public const int DefaultDailyGoal = 10;

    public static readonly IReadOnlyList<int> AllowedDailyGoals = [5, 10, 15];

    private readonly List<LessonCompletion> _completedLessons = [];

    private Learner()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>New words per day the learner committed to.</summary>
    public int DailyGoal { get; private set; }

    /// <summary>False when the learner does not know the Latin letters yet: they start with the foundations course.</summary>
    public bool KnowsLetters { get; private set; } = true;

    /// <summary>The grammar step the learner starts the main course from (1 is the beginning).</summary>
    public int StartStep { get; private set; } = 1;

    /// <summary>True once the learner answered the questions at sign-up.</summary>
    public bool OnboardingDone { get; private set; }

    public int CurrentStreak { get; private set; }

    public int LongestStreak { get; private set; }

    public DateOnly? LastStudyDate { get; private set; }

    public IReadOnlyCollection<LessonCompletion> CompletedLessons => _completedLessons;

    public static Learner Start(Guid id) => new() { Id = id, DailyGoal = DefaultDailyGoal };

    /// <exception cref="BusinessRuleException">The step is not one of the <see cref="StartingPoints"/>.</exception>
    public void ChooseStart(bool knowsLetters, int startStep)
    {
        if (!StartingPoints.IsValid(startStep))
        {
            throw new BusinessRuleException(LearningErrors.InvalidStartingPoint, "Choose one of the starting points offered.");
        }

        KnowsLetters = knowsLetters;
        StartStep = startStep;
        OnboardingDone = true;
    }

    public void SetDailyGoal(int wordsPerDay)
    {
        if (!AllowedDailyGoals.Contains(wordsPerDay))
        {
            throw new BusinessRuleException(
                LearningErrors.InvalidDailyGoal,
                $"Daily goal must be one of: {string.Join(", ", AllowedDailyGoals)} words.");
        }

        DailyGoal = wordsPerDay;
    }

    /// <param name="dueReviewCount">Words due for review today; too many pause new lessons.</param>
    /// <param name="backlog">How many due words are too many.</param>
    /// <returns>False when the lesson was already completed; completing it again changes nothing.</returns>
    public bool CompleteLesson(Guid lessonId, DateOnly today, int dueReviewCount, ReviewBacklogPolicy backlog)
    {
        if (_completedLessons.Any(c => c.LessonId == lessonId))
        {
            return false;
        }

        if (backlog.PausesNewLessons(dueReviewCount))
        {
            throw new BusinessRuleException(
                LearningErrors.ReviewBacklog,
                $"Review your words first: {dueReviewCount} are due, new lessons resume at {backlog.Limit} or fewer.");
        }

        _completedLessons.Add(new LessonCompletion(lessonId, today));
        RecordStudyDay(today);
        return true;
    }

    public bool CompletedAnyLessonOn(DateOnly day) => _completedLessons.Any(c => c.CompletedOn == day);

    /// <summary>Any study on a day keeps the streak: consecutive days grow it, a gap restarts it.</summary>
    public void RecordStudyDay(DateOnly today)
    {
        if (LastStudyDate == today)
        {
            return;
        }

        CurrentStreak = LastStudyDate == today.AddDays(-1) ? CurrentStreak + 1 : 1;
        LongestStreak = Math.Max(LongestStreak, CurrentStreak);
        LastStudyDate = today;
    }

    /// <summary>The streak as the learner sees it today: zero once a whole day was missed.</summary>
    public int StreakOn(DateOnly today) =>
        LastStudyDate is { } last && last >= today.AddDays(-1) ? CurrentStreak : 0;
}

public sealed record LessonCompletion(Guid LessonId, DateOnly CompletedOn);

using Wordrise.Modules.Learning.Domain.Lessons;

namespace Wordrise.Modules.Learning.Domain.Vocabulary;

public enum ReviewRating
{
    Hard,
    Good,
    Easy,
}

public enum WordStatus
{
    /// <summary>Introduced in a lesson, not reviewed yet.</summary>
    New,

    /// <summary>Its review day has come.</summary>
    Due,

    Learning,

    /// <summary>Remembered long enough that reviews are weeks apart.</summary>
    Mastered,
}

/// <summary>
/// How well one learner knows one word, scheduled by spaced repetition: each successful
/// review pushes the next one further away, a hard one brings it back to tomorrow.
/// </summary>
public sealed class WordProgress
{
    public const int MasteredIntervalDays = 21;

    private WordProgress()
    {
    }

    public Guid LearnerId { get; private set; }

    public Guid WordId { get; private set; }

    public Guid LessonId { get; private set; }

    public string English { get; private set; } = string.Empty;

    public string Arabic { get; private set; } = string.Empty;

    public int IntervalDays { get; private set; }

    public DateOnly DueOn { get; private set; }

    public int ReviewCount { get; private set; }

    public ReviewRating? LastRating { get; private set; }

    /// <summary>A word met in today's lesson is first reviewed tomorrow.</summary>
    public static WordProgress Introduce(Guid learnerId, Guid lessonId, LessonWord word, DateOnly today) => new()
    {
        LearnerId = learnerId,
        WordId = word.Id,
        LessonId = lessonId,
        English = word.English,
        Arabic = word.Arabic,
        DueOn = today.AddDays(1),
    };

    public void Review(ReviewRating rating, DateOnly today)
    {
        IntervalDays = rating switch
        {
            ReviewRating.Hard => 1,
            ReviewRating.Good => Math.Max(3, IntervalDays * 2),
            ReviewRating.Easy => Math.Max(7, IntervalDays * 3),
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, null),
        };
        DueOn = today.AddDays(IntervalDays);
        ReviewCount++;
        LastRating = rating;
    }

    public WordStatus StatusOn(DateOnly today)
    {
        if (DueOn <= today)
        {
            return WordStatus.Due;
        }

        if (ReviewCount == 0)
        {
            return WordStatus.New;
        }

        return IntervalDays >= MasteredIntervalDays ? WordStatus.Mastered : WordStatus.Learning;
    }
}

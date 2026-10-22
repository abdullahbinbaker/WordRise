using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Domain.Tests;

public sealed class WordProgressTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);

    private static WordProgress NewWord() =>
        WordProgress.Introduce(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new LessonWord { Id = Guid.NewGuid(), English = "milk", Arabic = "حليب" },
            Today);

    [Fact]
    public void A_new_word_is_first_reviewed_tomorrow()
    {
        var word = NewWord();

        Assert.Equal(Today.AddDays(1), word.DueOn);
        Assert.Equal(WordStatus.New, word.StatusOn(Today));
        Assert.Equal(WordStatus.Due, word.StatusOn(Today.AddDays(1)));
    }

    [Theory]
    [InlineData(ReviewRating.Hard, 1)]
    [InlineData(ReviewRating.Good, 3)]
    [InlineData(ReviewRating.Easy, 7)]
    public void The_first_review_schedules_by_how_hard_it_was(ReviewRating rating, int expectedDays)
    {
        var word = NewWord();

        word.Review(rating, Today);

        Assert.Equal(expectedDays, word.IntervalDays);
        Assert.Equal(Today.AddDays(expectedDays), word.DueOn);
    }

    [Fact]
    public void Good_reviews_double_the_gap_until_the_word_is_mastered()
    {
        var word = NewWord();
        var day = Today;
        var gaps = new List<int>();

        for (var i = 0; i < 4; i++)
        {
            word.Review(ReviewRating.Good, day);
            gaps.Add(word.IntervalDays);
            day = word.DueOn;
        }

        Assert.Equal([3, 6, 12, 24], gaps);
        Assert.Equal(WordStatus.Mastered, word.StatusOn(day.AddDays(-1)));
    }

    [Fact]
    public void A_hard_review_brings_a_learned_word_back_to_tomorrow()
    {
        var word = NewWord();
        word.Review(ReviewRating.Easy, Today);

        word.Review(ReviewRating.Hard, word.DueOn);

        Assert.Equal(1, word.IntervalDays);
        Assert.Equal(2, word.ReviewCount);
    }
}

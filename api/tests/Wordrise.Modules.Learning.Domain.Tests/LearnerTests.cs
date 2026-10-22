using Wordrise.Modules.Learning.Domain;
using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Domain.Tests;

public sealed class LearnerTests
{
    private static readonly DateOnly Monday = new(2026, 10, 19);

    private static readonly ReviewBacklogPolicy Backlog = ReviewBacklogPolicy.WithLimit(30);

    [Fact]
    public void A_new_learner_starts_with_the_default_goal_and_no_streak()
    {
        var learner = Learner.Start(Guid.NewGuid());

        Assert.Equal(Learner.DefaultDailyGoal, learner.DailyGoal);
        Assert.Equal(0, learner.StreakOn(Monday));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void Daily_goal_accepts_the_offered_values(int goal)
    {
        var learner = Learner.Start(Guid.NewGuid());

        learner.SetDailyGoal(goal);

        Assert.Equal(goal, learner.DailyGoal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(100)]
    public void Daily_goal_rejects_other_values(int goal)
    {
        var learner = Learner.Start(Guid.NewGuid());

        var error = Assert.Throws<BusinessRuleException>(() => learner.SetDailyGoal(goal));

        Assert.Equal(LearningErrors.InvalidDailyGoal, error.Code);
    }

    [Fact]
    public void Consecutive_days_grow_the_streak_and_the_same_day_counts_once()
    {
        var learner = Learner.Start(Guid.NewGuid());

        learner.RecordStudyDay(Monday);
        learner.RecordStudyDay(Monday);
        learner.RecordStudyDay(Monday.AddDays(1));
        learner.RecordStudyDay(Monday.AddDays(2));

        Assert.Equal(3, learner.CurrentStreak);
        Assert.Equal(3, learner.StreakOn(Monday.AddDays(2)));
    }

    [Fact]
    public void A_missed_day_restarts_the_streak_but_keeps_the_longest()
    {
        var learner = Learner.Start(Guid.NewGuid());
        learner.RecordStudyDay(Monday);
        learner.RecordStudyDay(Monday.AddDays(1));

        learner.RecordStudyDay(Monday.AddDays(3));

        Assert.Equal(1, learner.CurrentStreak);
        Assert.Equal(2, learner.LongestStreak);
    }

    [Fact]
    public void The_streak_shows_zero_once_a_whole_day_is_missed()
    {
        var learner = Learner.Start(Guid.NewGuid());
        learner.RecordStudyDay(Monday);

        Assert.Equal(1, learner.StreakOn(Monday.AddDays(1)));
        Assert.Equal(0, learner.StreakOn(Monday.AddDays(2)));
    }

    [Fact]
    public void Completing_a_lesson_twice_changes_nothing_the_second_time()
    {
        var learner = Learner.Start(Guid.NewGuid());
        var lessonId = Guid.NewGuid();

        var first = learner.CompleteLesson(lessonId, Monday, dueReviewCount: 0, Backlog);
        var second = learner.CompleteLesson(lessonId, Monday.AddDays(1), dueReviewCount: 0, Backlog);

        Assert.True(first);
        Assert.False(second);
        Assert.Single(learner.CompletedLessons);
        Assert.Equal(1, learner.CurrentStreak);
    }

    [Fact]
    public void A_review_backlog_above_the_limit_pauses_new_lessons()
    {
        var learner = Learner.Start(Guid.NewGuid());

        var error = Assert.Throws<BusinessRuleException>(
            () => learner.CompleteLesson(Guid.NewGuid(), Monday, dueReviewCount: 31, Backlog));

        Assert.Equal(LearningErrors.ReviewBacklog, error.Code);
        Assert.Empty(learner.CompletedLessons);
        Assert.Equal(0, learner.CurrentStreak);
    }

    [Fact]
    public void New_lessons_resume_once_the_backlog_is_at_the_limit()
    {
        var learner = Learner.Start(Guid.NewGuid());

        var completed = learner.CompleteLesson(Guid.NewGuid(), Monday, dueReviewCount: 30, Backlog);

        Assert.True(completed);
    }

    [Fact]
    public void The_backlog_limit_follows_the_configured_value()
    {
        var learner = Learner.Start(Guid.NewGuid());
        var strict = ReviewBacklogPolicy.WithLimit(5);

        Assert.Throws<BusinessRuleException>(() => learner.CompleteLesson(Guid.NewGuid(), Monday, dueReviewCount: 6, strict));
        Assert.True(learner.CompleteLesson(Guid.NewGuid(), Monday, dueReviewCount: 5, strict));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_backlog_limit_below_one_is_rejected(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReviewBacklogPolicy.WithLimit(limit));
    }

    [Fact]
    public void With_the_backlog_rule_off_new_lessons_never_pause()
    {
        var learner = Learner.Start(Guid.NewGuid());

        var completed = learner.CompleteLesson(Guid.NewGuid(), Monday, dueReviewCount: 500, ReviewBacklogPolicy.Off);

        Assert.True(completed);
        Assert.Null(ReviewBacklogPolicy.Off.Limit);
    }

    [Fact]
    public void Repeating_a_completed_lesson_is_harmless_even_with_a_backlog()
    {
        var learner = Learner.Start(Guid.NewGuid());
        var lessonId = Guid.NewGuid();
        learner.CompleteLesson(lessonId, Monday, dueReviewCount: 0, Backlog);

        var again = learner.CompleteLesson(lessonId, Monday.AddDays(5), dueReviewCount: 40, Backlog);

        Assert.False(again);
    }
}

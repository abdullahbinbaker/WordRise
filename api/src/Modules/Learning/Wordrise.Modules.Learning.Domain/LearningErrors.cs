namespace Wordrise.Modules.Learning.Domain;

/// <summary>Stable error codes clients can rely on.</summary>
public static class LearningErrors
{
    public const string InvalidDailyGoal = "learning.daily_goal_invalid";
    public const string ReviewBacklog = "learning.review_backlog";
    public const string ReviewsDisabled = "learning.reviews_disabled";
    public const string LessonNotFound = "learning.lesson_not_found";
    public const string LessonInvalid = "learning.lesson_invalid";
    public const string WordNotFound = "learning.word_not_found";
}

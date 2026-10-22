namespace Wordrise.Modules.Learning.Domain.Vocabulary;

/// <summary>
/// When too many reviews pile up (usually after time away), new lessons pause until the
/// learner catches up, so old words are not forgotten while new ones keep arriving.
/// The limit, and whether the rule applies at all, come from configuration.
/// </summary>
public sealed class ReviewBacklogPolicy
{
    public const int DefaultLimit = 30;

    private ReviewBacklogPolicy(int? limit) => Limit = limit;

    /// <summary>The rule is switched off: new lessons never pause.</summary>
    public static ReviewBacklogPolicy Off { get; } = new(null);

    /// <summary>More due words than this and the day is reviews only; null when the rule is off.</summary>
    public int? Limit { get; }

    public static ReviewBacklogPolicy WithLimit(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        return new ReviewBacklogPolicy(limit);
    }

    public bool PausesNewLessons(int dueReviewCount) => Limit is { } limit && dueReviewCount > limit;
}

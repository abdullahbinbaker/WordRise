using System.ComponentModel.DataAnnotations;
using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Infrastructure;

/// <summary>The <c>Learning</c> section of appsettings.</summary>
public sealed class LearningOptions
{
    public const string SectionName = "Learning";

    /// <summary>IANA time zone whose midnight starts a new learning day.</summary>
    [Required]
    public string TimeZone { get; set; } = "Asia/Riyadh";

    public ReviewOptions Reviews { get; set; } = new();
}

/// <summary><c>Learning:Reviews</c>: spaced-repetition reviews.</summary>
public sealed class ReviewOptions
{
    public const int MinPerSession = 1;
    public const int MaxPerSessionLimit = 200;

    /// <summary>False turns reviews off: lessons go straight to new words and nothing is due.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Most words reviewed in one session; the rest wait for the next one.</summary>
    public int MaxPerSession { get; set; } = 20;

    public ReviewBacklogOptions Backlog { get; set; } = new();
}

/// <summary><c>Learning:Reviews:Backlog</c>: pausing new lessons when reviews pile up.</summary>
public sealed class ReviewBacklogOptions
{
    public const int MinLimit = 1;
    public const int MaxLimit = 1000;

    /// <summary>False lets learners start new lessons however many reviews are due.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>More due words than this pauses new lessons until the learner catches up.</summary>
    public int Limit { get; set; } = ReviewBacklogPolicy.DefaultLimit;
}

namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <summary>What a reviewer checks. The same seven points decide every lesson.</summary>
public enum ReviewCriterion
{
    /// <summary>No spelling, grammar or translation mistakes, in English or Arabic.</summary>
    LanguageAccuracy,

    /// <summary>Words and sentences suit the lesson's CEFR level.</summary>
    LevelMatch,

    /// <summary>Only the allowed grammar and known words are used, plus the lesson's new words.</summary>
    VocabularyAndGrammarAdherence,

    AgeAppropriateness,

    /// <summary>Suits Arabic-speaking Muslim learners: nothing against their values or customs.</summary>
    CulturalFit,

    /// <summary>People really talk like this in this situation.</summary>
    Realism,

    /// <summary>Each question has one clear correct answer and tests what was taught.</summary>
    ExerciseQuality,
}

public enum IssueSeverity
{
    /// <summary>Worth improving, but the lesson is fine to publish.</summary>
    Suggestion,

    /// <summary>The lesson must not be published until this is fixed.</summary>
    Blocking,
}

/// <summary>Who produced the report.</summary>
public enum ReviewSource
{
    /// <summary>Plain code checks: free, run before any AI reviewer is paid for.</summary>
    Structural,

    Ai,
}

public sealed class CriterionResult
{
    public ReviewCriterion Criterion { get; init; }

    public bool Passed { get; init; }

    public string Comment { get; init; } = string.Empty;
}

public sealed class ReviewIssue
{
    public IssueSeverity Severity { get; init; }

    /// <summary>Where in the lesson: "word: milk", "dialogue line 3".</summary>
    public string Where { get; init; } = string.Empty;

    public string Problem { get; init; } = string.Empty;

    public string Suggestion { get; init; } = string.Empty;
}

/// <summary>The verdict on one version of a lesson, with the reasons the writer needs to fix it.</summary>
public sealed class ReviewReport
{
    private ReviewReport()
    {
    }

    public ReviewSource Source { get; private set; }

    public bool Passed { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public List<CriterionResult> Criteria { get; private set; } = [];

    public List<ReviewIssue> Issues { get; private set; } = [];

    /// <summary>
    /// An AI verdict. A point the reviewer skipped counts as failed, so a lazy answer can never
    /// approve a lesson; see <see cref="Decide"/> for when a lesson passes.
    /// </summary>
    public static ReviewReport FromAi(
        string summary,
        IEnumerable<CriterionResult> criteria,
        IEnumerable<ReviewIssue> issues)
    {
        var given = criteria.GroupBy(c => c.Criterion).ToDictionary(g => g.Key, g => g.Last());
        var missing = Enum.GetValues<ReviewCriterion>().Count(criterion => !given.ContainsKey(criterion));
        List<CriterionResult> complete =
        [
            .. Enum.GetValues<ReviewCriterion>().Select(criterion =>
                given.TryGetValue(criterion, out var result)
                    ? result
                    : new CriterionResult { Criterion = criterion, Passed = false, Comment = "The reviewer did not evaluate this point." }),
        ];
        var issueList = issues.ToList();

        return new ReviewReport
        {
            Source = ReviewSource.Ai,
            Summary = summary,
            Criteria = complete,
            Issues = issueList,
            Passed = Decide(missing, complete, issueList),
        };
    }

    /// <summary>
    /// The lesson passes when nothing blocks it. Suggestions never block: they are shown to the person
    /// and sent to the writer, but a lesson is not rejected for being improvable. Two things stay strict,
    /// because they mean the verdict cannot be trusted: a point the reviewer did not evaluate, and a point
    /// it marked failed without reporting a single issue to explain it.
    /// </summary>
    private static bool Decide(int missingPoints, IReadOnlyList<CriterionResult> criteria, IReadOnlyList<ReviewIssue> issues)
    {
        if (missingPoints > 0 || issues.Any(issue => issue.Severity == IssueSeverity.Blocking))
        {
            return false;
        }

        var failedWithoutExplanation = criteria.Any(c => !c.Passed) && issues.Count == 0;
        return !failedWithoutExplanation;
    }

    /// <summary>A failed verdict from the free code checks.</summary>
    public static ReviewReport FromStructuralProblems(IReadOnlyList<ContentProblem> problems) => new()
    {
        Source = ReviewSource.Structural,
        Passed = problems.Count == 0,
        Summary = problems.Count == 0 ? "The lesson is well formed." : "The lesson is incomplete or malformed.",
        Issues =
        [
            .. problems.Select(p => new ReviewIssue
            {
                Severity = IssueSeverity.Blocking,
                Where = p.Where,
                Problem = p.Message,
            }),
        ],
    };
}

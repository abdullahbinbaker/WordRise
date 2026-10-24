using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.TestSupport;

namespace Wordrise.Modules.ContentAuthoring.Domain.Tests;

public sealed class ReviewReportTests
{
    private static IEnumerable<CriterionResult> AllPassing() =>
        Enum.GetValues<ReviewCriterion>().Select(c => new CriterionResult { Criterion = c, Passed = true });

    [Fact]
    public void A_report_passes_when_every_point_passes_and_nothing_blocks()
    {
        var report = ReviewReport.FromAi("ok", AllPassing(), []);

        Assert.True(report.Passed);
        Assert.Equal(ReviewSource.Ai, report.Source);
        Assert.Equal(Enum.GetValues<ReviewCriterion>().Length, report.Criteria.Count);
    }

    [Fact]
    public void One_failed_point_fails_the_report()
    {
        var criteria = AllPassing().Select(c => c.Criterion == ReviewCriterion.CulturalFit
            ? new CriterionResult { Criterion = c.Criterion, Passed = false, Comment = "Mentions pork." }
            : c);

        var report = ReviewReport.FromAi("no", criteria, []);

        Assert.False(report.Passed);
    }

    [Fact]
    public void A_point_the_reviewer_skipped_counts_as_failed()
    {
        var onlyTwo = AllPassing().Take(2);

        var report = ReviewReport.FromAi("lazy", onlyTwo, []);

        Assert.False(report.Passed);
        Assert.Equal(Enum.GetValues<ReviewCriterion>().Length, report.Criteria.Count);
        Assert.Equal(Enum.GetValues<ReviewCriterion>().Length - 2, report.Criteria.Count(c => !c.Passed));
    }

    [Fact]
    public void A_blocking_issue_fails_the_report_even_when_every_point_passes()
    {
        var issue = new ReviewIssue { Severity = IssueSeverity.Blocking, Problem = "Wrong translation." };

        Assert.False(ReviewReport.FromAi("no", AllPassing(), [issue]).Passed);
    }

    [Fact]
    public void A_suggestion_alone_does_not_fail_the_report()
    {
        var issue = new ReviewIssue { Severity = IssueSeverity.Suggestion, Problem = "Could be shorter." };

        Assert.True(ReviewReport.FromAi("ok", AllPassing(), [issue]).Passed);
    }

    private static IEnumerable<CriterionResult> AllPassingExcept(ReviewCriterion failed) =>
        Enum.GetValues<ReviewCriterion>().Select(c => new CriterionResult { Criterion = c, Passed = c != failed });

    [Fact]
    public void A_point_marked_failed_for_suggestions_only_does_not_reject_the_lesson()
    {
        // The reviewer used to fail whole points over style remarks, so lessons never passed.
        var suggestion = new ReviewIssue { Severity = IssueSeverity.Suggestion, Where = "listening", Problem = "Options could be more alike." };

        var report = ReviewReport.FromAi("fine", AllPassingExcept(ReviewCriterion.ExerciseQuality), [suggestion]);

        Assert.True(report.Passed);
        Assert.Single(report.Issues);
    }

    [Fact]
    public void A_point_marked_failed_with_a_blocking_issue_rejects_the_lesson()
    {
        var blocking = new ReviewIssue { Severity = IssueSeverity.Blocking, Where = "dialogue line 4", Problem = "Wrong translation." };

        var report = ReviewReport.FromAi("no", AllPassingExcept(ReviewCriterion.LanguageAccuracy), [blocking]);

        Assert.False(report.Passed);
    }

    [Fact]
    public void A_point_marked_failed_with_no_explanation_at_all_still_rejects_the_lesson()
    {
        // A failure nobody can read cannot be trusted either way, so it is not waved through.
        Assert.False(ReviewReport.FromAi("no", AllPassingExcept(ReviewCriterion.CulturalFit), []).Passed);
    }

    [Fact]
    public void Many_suggestions_with_every_point_passing_still_pass()
    {
        ReviewIssue[] suggestions =
        [
            .. Enumerable.Range(1, 6).Select(i => new ReviewIssue { Severity = IssueSeverity.Suggestion, Problem = $"Idea {i}." }),
        ];

        Assert.True(ReviewReport.FromAi("fine", AllPassing(), suggestions).Passed);
    }

    [Fact]
    public void A_criterion_given_twice_keeps_the_last_verdict()
    {
        var criteria = AllPassing().Append(new CriterionResult { Criterion = ReviewCriterion.Realism, Passed = false });

        Assert.False(ReviewReport.FromAi("x", criteria, []).Passed);
    }

    [Fact]
    public void Structural_problems_become_blocking_issues_and_fail_the_report()
    {
        var report = ReviewReport.FromStructuralProblems([new ContentProblem("title", "This is empty.")]);

        Assert.False(report.Passed);
        Assert.Equal(ReviewSource.Structural, report.Source);
        var issue = Assert.Single(report.Issues);
        Assert.Equal(IssueSeverity.Blocking, issue.Severity);
        Assert.Equal("title", issue.Where);
    }
}

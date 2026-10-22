using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;

public sealed record DraftListItemDto(
    Guid Id,
    string Scenario,
    int GrammarStep,
    string GrammarPoint,
    DraftStatus Status,
    int RevisionCount,
    int? PublishedLessonNumber,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BriefDto(
    string Scenario,
    int GrammarStep,
    string GrammarPoint,
    string Level,
    int WordCount,
    IReadOnlyList<string> AllowedGrammar);

public sealed record DraftDetailDto(
    Guid Id,
    BriefDto Brief,
    DraftStatus Status,
    string? Error,
    int CyclesUsed,
    DraftContentDto? Content,
    IReadOnlyList<RevisionDto> Revisions,
    IReadOnlyList<FeedbackDto> Feedback,
    Guid? PublishedLessonId,
    int? PublishedLessonNumber,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record RevisionDto(
    int Number,
    bool Passed,
    ReviewSource Source,
    string Summary,
    IReadOnlyList<CriterionResultDto> Criteria,
    IReadOnlyList<IssueDto> Issues,
    DateTime CreatedAtUtc);

public sealed record CriterionResultDto(ReviewCriterion Criterion, bool Passed, string Comment);

public sealed record IssueDto(IssueSeverity Severity, string Where, string Problem, string Suggestion);

public sealed record FeedbackDto(Guid By, string Text, DateTime AtUtc);

public sealed record DraftContentDto(
    string Title,
    IReadOnlyList<DraftWordDto> Words,
    IReadOnlyList<DraftDialogueLineDto> Dialogue,
    string ChatPrompt,
    IReadOnlyList<DraftAnswerOptionDto> ChatOptions,
    DraftGrammarDto Grammar,
    string ListeningSentence,
    IReadOnlyList<DraftAnswerOptionDto> ListeningOptions);

public sealed record DraftWordDto(string English, string Arabic, string Pronunciation, string Example, string ExampleArabic);

public sealed record DraftDialogueLineDto(string Speaker, bool IsLearner, string English, string Arabic);

public sealed record DraftAnswerOptionDto(string Text, bool IsCorrect);

public sealed record DraftGrammarDto(
    string Title,
    string Meaning,
    string Explanation,
    string Pattern,
    IReadOnlyList<DraftGrammarExampleDto> Examples,
    string Note);

public sealed record DraftGrammarExampleDto(string English, string Arabic);

public sealed record PublishedLessonDto(Guid LessonId, int Number);

public sealed record GrammarPointDto(int Number, string Title, string Level);

/// <summary>Turns the aggregate into the shapes the admin screens use.</summary>
public static class DraftMapping
{
    public static DraftListItemDto ToListItem(LessonDraft draft) => new(
        draft.Id,
        draft.Brief.Scenario,
        draft.Brief.GrammarStep,
        draft.Brief.Grammar.Title,
        draft.Status,
        draft.Revisions.Count,
        draft.PublishedLessonNumber,
        draft.CreatedAtUtc,
        draft.UpdatedAtUtc);

    public static DraftDetailDto ToDetail(LessonDraft draft) => new(
        draft.Id,
        new BriefDto(
            draft.Brief.Scenario,
            draft.Brief.GrammarStep,
            draft.Brief.Grammar.Title,
            draft.Brief.Level,
            draft.Brief.WordCount,
            draft.Brief.AllowedGrammar),
        draft.Status,
        draft.Error,
        draft.CyclesUsed,
        draft.CurrentContent is { } content ? ToContent(content) : null,
        [.. draft.Revisions.Select(ToRevision)],
        [.. draft.Feedback.Select(f => new FeedbackDto(f.By, f.Text, f.AtUtc))],
        draft.PublishedLessonId,
        draft.PublishedLessonNumber,
        draft.CreatedAtUtc,
        draft.UpdatedAtUtc);

    private static RevisionDto ToRevision(DraftRevision revision) => new(
        revision.Number,
        revision.Review.Passed,
        revision.Review.Source,
        revision.Review.Summary,
        [.. revision.Review.Criteria.Select(c => new CriterionResultDto(c.Criterion, c.Passed, c.Comment))],
        [.. revision.Review.Issues.Select(i => new IssueDto(i.Severity, i.Where, i.Problem, i.Suggestion))],
        revision.CreatedAtUtc);

    private static DraftContentDto ToContent(DraftContent c) => new(
        c.Title,
        [.. c.Words.Select(w => new DraftWordDto(w.English, w.Arabic, w.Pronunciation, w.Example, w.ExampleArabic))],
        [.. c.Dialogue.Select(d => new DraftDialogueLineDto(d.Speaker, d.IsLearner, d.English, d.Arabic))],
        c.ChatPrompt,
        [.. c.ChatOptions.Select(o => new DraftAnswerOptionDto(o.Text, o.IsCorrect))],
        new DraftGrammarDto(
            c.Grammar.Title,
            c.Grammar.Meaning,
            c.Grammar.Explanation,
            c.Grammar.Pattern,
            [.. c.Grammar.Examples.Select(e => new DraftGrammarExampleDto(e.English, e.Arabic))],
            c.Grammar.Note),
        c.ListeningSentence,
        [.. c.ListeningOptions.Select(o => new DraftAnswerOptionDto(o.Text, o.IsCorrect))]);
}

using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Claude;

// The JSON shapes Claude is asked to return (see ClaudeSchemas). They are translated to the
// domain here, so a change to the prompt or the schema never reaches the rest of the module.

internal sealed record LessonJson(
    string Title,
    List<WordJson> Words,
    List<DialogueLineJson> Dialogue,
    string ChatPrompt,
    List<OptionJson> ChatOptions,
    GrammarJson Grammar,
    string ListeningSentence,
    List<OptionJson> ListeningOptions)
{
    public static LessonJson From(DraftContent c) => new(
        c.Title,
        [.. c.Words.Select(w => new WordJson(w.English, w.Arabic, w.Pronunciation, w.Example, w.ExampleArabic))],
        [.. c.Dialogue.Select(d => new DialogueLineJson(d.Speaker, d.IsLearner, d.English, d.Arabic))],
        c.ChatPrompt,
        [.. c.ChatOptions.Select(o => new OptionJson(o.Text, o.IsCorrect))],
        new GrammarJson(
            c.Grammar.Title,
            c.Grammar.Meaning,
            c.Grammar.Explanation,
            c.Grammar.Pattern,
            [.. c.Grammar.Examples.Select(e => new GrammarExampleJson(e.English, e.Arabic))],
            c.Grammar.Note),
        c.ListeningSentence,
        [.. c.ListeningOptions.Select(o => new OptionJson(o.Text, o.IsCorrect))]);

    public DraftContent ToDomain() => new()
    {
        Title = Title ?? string.Empty,
        Words =
        [
            .. (Words ?? []).Select(w => new DraftWord
            {
                English = w.English ?? string.Empty,
                Arabic = w.Arabic ?? string.Empty,
                Pronunciation = w.Pronunciation ?? string.Empty,
                Example = w.Example ?? string.Empty,
                ExampleArabic = w.ExampleArabic ?? string.Empty,
            }),
        ],
        Dialogue =
        [
            .. (Dialogue ?? []).Select(d => new DraftDialogueLine
            {
                Speaker = d.Speaker ?? string.Empty,
                IsLearner = d.IsLearner,
                English = d.English ?? string.Empty,
                Arabic = d.Arabic ?? string.Empty,
            }),
        ],
        ChatPrompt = ChatPrompt ?? string.Empty,
        ChatOptions = [.. (ChatOptions ?? []).Select(o => new DraftAnswerOption { Text = o.Text ?? string.Empty, IsCorrect = o.IsCorrect })],
        Grammar = new DraftGrammar
        {
            Title = Grammar?.Title ?? string.Empty,
            Meaning = Grammar?.Meaning ?? string.Empty,
            Explanation = Grammar?.Explanation ?? string.Empty,
            Pattern = Grammar?.Pattern ?? string.Empty,
            Examples =
            [
                .. (Grammar?.Examples ?? []).Select(e => new DraftGrammarExample
                {
                    English = e.English ?? string.Empty,
                    Arabic = e.Arabic ?? string.Empty,
                }),
            ],
            Note = Grammar?.Note ?? string.Empty,
        },
        ListeningSentence = ListeningSentence ?? string.Empty,
        ListeningOptions = [.. (ListeningOptions ?? []).Select(o => new DraftAnswerOption { Text = o.Text ?? string.Empty, IsCorrect = o.IsCorrect })],
    };
}

internal sealed record WordJson(string English, string Arabic, string Pronunciation, string Example, string ExampleArabic);

internal sealed record DialogueLineJson(string Speaker, bool IsLearner, string English, string Arabic);

internal sealed record OptionJson(string Text, bool IsCorrect);

internal sealed record GrammarJson(
    string Title,
    string Meaning,
    string Explanation,
    string Pattern,
    List<GrammarExampleJson> Examples,
    string Note);

internal sealed record GrammarExampleJson(string English, string Arabic);

internal sealed record ReviewJson(string Summary, List<CriterionJson> Criteria, List<IssueJson> Issues)
{
    public ReviewReport ToDomain() => ReviewReport.FromAi(
        Summary ?? string.Empty,
        (Criteria ?? [])
            .Where(c => Enum.TryParse<ReviewCriterion>(c.Criterion, ignoreCase: true, out _))
            .Select(c => new CriterionResult
            {
                Criterion = Enum.Parse<ReviewCriterion>(c.Criterion, ignoreCase: true),
                Passed = c.Passed,
                Comment = c.Comment ?? string.Empty,
            }),
        (Issues ?? []).Select(i => new ReviewIssue
        {
            Severity = string.Equals(i.Severity, "blocking", StringComparison.OrdinalIgnoreCase)
                ? IssueSeverity.Blocking
                : IssueSeverity.Suggestion,
            Where = i.Where ?? string.Empty,
            Problem = i.Problem ?? string.Empty,
            Suggestion = i.Suggestion ?? string.Empty,
        }));
}

internal sealed record CriterionJson(string Criterion, bool Passed, string Comment);

internal sealed record IssueJson(string Severity, string Where, string Problem, string Suggestion);

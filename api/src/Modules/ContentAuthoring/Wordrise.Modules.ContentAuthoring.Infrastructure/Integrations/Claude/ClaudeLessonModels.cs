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
        [.. c.Words.Select(w => new WordJson(w.Target, w.Native, w.Pronunciation, w.Example, w.ExampleNative))],
        [.. c.Dialogue.Select(d => new DialogueLineJson(d.Speaker, d.IsLearner, d.Target, d.Native))],
        c.ChatPrompt,
        [.. c.ChatOptions.Select(o => new OptionJson(o.Text, o.IsCorrect))],
        new GrammarJson(
            c.Grammar.Title,
            c.Grammar.Meaning,
            c.Grammar.Explanation,
            c.Grammar.Pattern,
            [.. c.Grammar.Examples.Select(e => new GrammarExampleJson(e.Target, e.Native))],
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
                Target = w.Target ?? string.Empty,
                Native = w.Native ?? string.Empty,
                Pronunciation = w.Pronunciation ?? string.Empty,
                Example = w.Example ?? string.Empty,
                ExampleNative = w.ExampleNative ?? string.Empty,
            }),
        ],
        Dialogue =
        [
            .. (Dialogue ?? []).Select(d => new DraftDialogueLine
            {
                Speaker = d.Speaker ?? string.Empty,
                IsLearner = d.IsLearner,
                Target = d.Target ?? string.Empty,
                Native = d.Native ?? string.Empty,
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
                    Target = e.Target ?? string.Empty,
                    Native = e.Native ?? string.Empty,
                }),
            ],
            Note = Grammar?.Note ?? string.Empty,
        },
        ListeningSentence = ListeningSentence ?? string.Empty,
        ListeningOptions = [.. (ListeningOptions ?? []).Select(o => new DraftAnswerOption { Text = o.Text ?? string.Empty, IsCorrect = o.IsCorrect })],
    };
}

internal sealed record WordJson(string Target, string Native, string Pronunciation, string Example, string ExampleNative);

internal sealed record DialogueLineJson(string Speaker, bool IsLearner, string Target, string Native);

internal sealed record OptionJson(string Text, bool IsCorrect);

internal sealed record GrammarJson(
    string Title,
    string Meaning,
    string Explanation,
    string Pattern,
    List<GrammarExampleJson> Examples,
    string Note);

internal sealed record GrammarExampleJson(string Target, string Native);

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

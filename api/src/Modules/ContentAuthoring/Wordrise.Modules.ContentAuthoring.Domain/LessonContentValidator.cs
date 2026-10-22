using System.Text.RegularExpressions;

namespace Wordrise.Modules.ContentAuthoring.Domain;

public sealed record ContentProblem(string Where, string Message);

/// <summary>
/// Free, instant checks of a lesson's shape. They catch what a writer gets wrong mechanically
/// (a missing word, two correct answers, English where Arabic belongs) so the paid AI reviewer
/// only sees lessons worth reading.
/// </summary>
public static partial class LessonContentValidator
{
    public const int MinDialogueLines = 3;
    public const int MaxDialogueLines = 10;
    public const int MinGrammarExamples = 3;

    public static IReadOnlyList<ContentProblem> Validate(LessonBrief brief, DraftContent content)
    {
        List<ContentProblem> problems = [];
        void Add(string where, string message) => problems.Add(new ContentProblem(where, message));

        RequireText(content.Title, "title", Add, arabic: true);

        ValidateWords(brief, content, Add);
        ValidateDialogue(content, Add);

        RequireText(content.ChatPrompt, "dialogue question", Add, arabic: true);
        ValidateOptions(content.ChatOptions, "dialogue question", minimum: 2, maximum: 4, Add);

        ValidateGrammar(content.Grammar, Add);

        RequireText(content.ListeningSentence, "listening sentence", Add, latin: true);
        ValidateOptions(content.ListeningOptions, "listening exercise", minimum: 3, maximum: 4, Add);
        var answer = content.ListeningOptions.FirstOrDefault(o => o.IsCorrect)?.Text;
        if (!string.IsNullOrWhiteSpace(answer) && !ContainsWord(content.ListeningSentence, answer))
        {
            Add("listening exercise", $"The correct answer \"{answer}\" is not in the sentence the learner hears.");
        }

        return problems;
    }

    private static void ValidateWords(LessonBrief brief, DraftContent content, Action<string, string> add)
    {
        if (content.Words.Count != brief.WordCount)
        {
            add("words", $"The lesson must teach exactly {brief.WordCount} new words, but has {content.Words.Count}.");
        }

        var dialogueText = string.Join(' ', content.Dialogue.Select(d => d.English));
        foreach (var word in content.Words)
        {
            var where = $"word: {word.English}";
            RequireText(word.English, where, add, latin: true);
            RequireText(word.Arabic, $"{where} (Arabic meaning)", add, arabic: true);
            RequireText(word.Pronunciation, $"{where} (pronunciation)", add, arabic: true);
            RequireText(word.Example, $"{where} (example)", add, latin: true);
            RequireText(word.ExampleArabic, $"{where} (example translation)", add, arabic: true);

            if (!string.IsNullOrWhiteSpace(word.English) && !ContainsWord(dialogueText, word.English.Trim().TrimEnd('?', '.', '!')))
            {
                add(where, "The dialogue never uses this word; every new word must appear in the dialogue.");
            }
        }

        foreach (var repeated in content.Words
            .GroupBy(w => w.English.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1))
        {
            add($"word: {repeated.Key}", "The same word is listed twice.");
        }
    }

    private static void ValidateDialogue(DraftContent content, Action<string, string> add)
    {
        var lines = content.Dialogue;
        if (lines.Count is < MinDialogueLines or > MaxDialogueLines)
        {
            add("dialogue", $"The dialogue needs {MinDialogueLines} to {MaxDialogueLines} lines, but has {lines.Count}.");
        }

        if (lines.Count > 0 && !lines.Any(l => l.IsLearner))
        {
            add("dialogue", "The learner never speaks: at least one line must be the learner's.");
        }

        if (lines.Count > 0 && lines.All(l => l.IsLearner))
        {
            add("dialogue", "The learner needs someone to talk to: at least one line must be the other speaker's.");
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var where = $"dialogue line {i + 1}";
            RequireText(lines[i].Speaker, $"{where} (speaker)", add);
            RequireText(lines[i].English, where, add, latin: true);
            RequireText(lines[i].Arabic, $"{where} (translation)", add, arabic: true);
        }
    }

    private static void ValidateGrammar(DraftGrammar grammar, Action<string, string> add)
    {
        RequireText(grammar.Title, "grammar title", add, latin: true);
        RequireText(grammar.Meaning, "grammar meaning", add, arabic: true);
        RequireText(grammar.Explanation, "grammar explanation", add, arabic: true);
        RequireText(grammar.Pattern, "grammar pattern", add);

        if (grammar.Examples.Count < MinGrammarExamples)
        {
            add("grammar", $"The grammar point needs at least {MinGrammarExamples} examples, but has {grammar.Examples.Count}.");
        }

        for (var i = 0; i < grammar.Examples.Count; i++)
        {
            RequireText(grammar.Examples[i].English, $"grammar example {i + 1}", add, latin: true);
            RequireText(grammar.Examples[i].Arabic, $"grammar example {i + 1} (translation)", add, arabic: true);
        }
    }

    private static void ValidateOptions(
        List<DraftAnswerOption> options,
        string where,
        int minimum,
        int maximum,
        Action<string, string> add)
    {
        if (options.Count < minimum || options.Count > maximum)
        {
            add(where, $"It needs {minimum} to {maximum} answer options, but has {options.Count}.");
        }

        var correct = options.Count(o => o.IsCorrect);
        if (correct != 1)
        {
            add(where, $"It needs exactly one correct answer, but has {correct}.");
        }

        foreach (var option in options)
        {
            RequireText(option.Text, $"{where} (option)", add, latin: true);
        }

        foreach (var repeated in options.GroupBy(o => o.Text.Trim(), StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            add(where, $"The option \"{repeated.Key}\" appears twice.");
        }
    }

    /// <summary>A field must be filled, and written in the script its language needs.</summary>
    private static void RequireText(string? text, string where, Action<string, string> add, bool arabic = false, bool latin = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            add(where, "This is empty.");
        }
        else if (arabic && !ArabicLetter().IsMatch(text))
        {
            add(where, "This must be written in Arabic.");
        }
        else if (latin && !LatinLetter().IsMatch(text))
        {
            add(where, "This must be written in English.");
        }
    }

    /// <summary>Whether the text uses the word or phrase, ignoring case (and endings like "apples").</summary>
    private static bool ContainsWord(string text, string word) =>
        Regex.IsMatch(text, $@"\b{Regex.Escape(word.Trim())}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [GeneratedRegex(@"[؀-ۿ]")]
    private static partial Regex ArabicLetter();

    [GeneratedRegex(@"[A-Za-z]")]
    private static partial Regex LatinLetter();
}

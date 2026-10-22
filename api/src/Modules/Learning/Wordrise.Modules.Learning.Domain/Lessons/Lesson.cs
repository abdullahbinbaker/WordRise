using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Domain.Lessons;

/// <summary>
/// One daily lesson built around a real-life scenario and one grammar point.
/// Published by content authoring; read-only for learners.
/// </summary>
public sealed class Lesson
{
    private Lesson()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Position in the course; learners take lessons in this order.</summary>
    public int Number { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Scenario { get; private set; } = string.Empty;

    public string GrammarPoint { get; private set; } = string.Empty;

    public LessonContent Content { get; private set; } = new();

    /// <exception cref="BusinessRuleException">The lesson could not be taught: a learner would hit a broken screen.</exception>
    public static Lesson Create(
        Guid id,
        int number,
        string title,
        string scenario,
        string grammarPoint,
        LessonContent content)
    {
        EnsureTeachable(title, scenario, grammarPoint, content);
        return new Lesson
        {
            Id = id,
            Number = number,
            Title = title.Trim(),
            Scenario = scenario.Trim(),
            GrammarPoint = grammarPoint.Trim(),
            Content = content,
        };
    }

    private static void EnsureTeachable(string title, string scenario, string grammarPoint, LessonContent content)
    {
        var problem =
            string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(scenario) || string.IsNullOrWhiteSpace(grammarPoint)
                ? "A lesson needs a title, a scenario and a grammar point."
            : content.Words.Count == 0
                ? "A lesson needs at least one word."
            : content.Words.Select(w => w.Id).Distinct().Count() != content.Words.Count
                ? "A word appears twice in the same lesson."
            : content.Dialogue.Count == 0
                ? "A lesson needs a dialogue."
            : !HasExactlyOneCorrectAnswer(content.ChatOptions)
                ? "The dialogue question needs at least two options and exactly one correct answer."
            : !HasExactlyOneCorrectAnswer(content.ListeningOptions)
                ? "The listening exercise needs at least two options and exactly one correct answer."
            : null;

        if (problem is not null)
        {
            throw new BusinessRuleException(LearningErrors.LessonInvalid, problem);
        }
    }

    private static bool HasExactlyOneCorrectAnswer(List<AnswerOption> options) =>
        options.Count >= 2 && options.Count(o => o.IsCorrect) == 1;
}

/// <summary>Everything the lesson screens show, in their order.</summary>
public sealed class LessonContent
{
    public List<LessonWord> Words { get; init; } = [];

    public List<DialogueLine> Dialogue { get; init; } = [];

    public string ChatPrompt { get; init; } = string.Empty;

    public List<AnswerOption> ChatOptions { get; init; } = [];

    public GrammarExplanation Grammar { get; init; } = new();

    public string ListeningSentence { get; init; } = string.Empty;

    public List<AnswerOption> ListeningOptions { get; init; } = [];
}

public sealed class LessonWord
{
    public Guid Id { get; init; }

    public string English { get; init; } = string.Empty;

    public string Arabic { get; init; } = string.Empty;

    /// <summary>How the word sounds, written in Arabic letters.</summary>
    public string Pronunciation { get; init; } = string.Empty;

    public string Example { get; init; } = string.Empty;

    public string ExampleArabic { get; init; } = string.Empty;
}

public sealed class DialogueLine
{
    public string Speaker { get; init; } = string.Empty;

    /// <summary>True when the learner says this line.</summary>
    public bool IsLearner { get; init; }

    public string English { get; init; } = string.Empty;

    public string Arabic { get; init; } = string.Empty;
}

public sealed class AnswerOption
{
    public string Text { get; init; } = string.Empty;

    public bool IsCorrect { get; init; }
}

public sealed class GrammarExplanation
{
    public string Title { get; init; } = string.Empty;

    public string Meaning { get; init; } = string.Empty;

    public string Explanation { get; init; } = string.Empty;

    public string Pattern { get; init; } = string.Empty;

    public List<GrammarExample> Examples { get; init; } = [];

    public string Note { get; init; } = string.Empty;
}

public sealed class GrammarExample
{
    public string English { get; init; } = string.Empty;

    public string Arabic { get; init; } = string.Empty;
}

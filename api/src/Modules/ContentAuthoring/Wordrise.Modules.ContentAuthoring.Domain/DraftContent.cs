namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <summary>
/// A lesson as the writer produced it, in the order its screens show it. Authoring owns this
/// shape; it is translated when the lesson is published to learners.
/// </summary>
public sealed class DraftContent
{
    /// <summary>The lesson title, in Arabic.</summary>
    public string Title { get; init; } = string.Empty;

    public List<DraftWord> Words { get; init; } = [];

    public List<DraftDialogueLine> Dialogue { get; init; } = [];

    /// <summary>The question after the dialogue, in Arabic.</summary>
    public string ChatPrompt { get; init; } = string.Empty;

    public List<DraftAnswerOption> ChatOptions { get; init; } = [];

    public DraftGrammar Grammar { get; init; } = new();

    public string ListeningSentence { get; init; } = string.Empty;

    public List<DraftAnswerOption> ListeningOptions { get; init; } = [];
}

public sealed class DraftWord
{
    public string English { get; init; } = string.Empty;

    public string Arabic { get; init; } = string.Empty;

    /// <summary>How the word sounds, written in Arabic letters.</summary>
    public string Pronunciation { get; init; } = string.Empty;

    public string Example { get; init; } = string.Empty;

    public string ExampleArabic { get; init; } = string.Empty;
}

public sealed class DraftDialogueLine
{
    public string Speaker { get; init; } = string.Empty;

    /// <summary>True when the learner says this line.</summary>
    public bool IsLearner { get; init; }

    public string English { get; init; } = string.Empty;

    public string Arabic { get; init; } = string.Empty;
}

public sealed class DraftAnswerOption
{
    public string Text { get; init; } = string.Empty;

    public bool IsCorrect { get; init; }
}

public sealed class DraftGrammar
{
    public string Title { get; init; } = string.Empty;

    public string Meaning { get; init; } = string.Empty;

    public string Explanation { get; init; } = string.Empty;

    public string Pattern { get; init; } = string.Empty;

    public List<DraftGrammarExample> Examples { get; init; } = [];

    public string Note { get; init; } = string.Empty;
}

public sealed class DraftGrammarExample
{
    public string English { get; init; } = string.Empty;

    public string Arabic { get; init; } = string.Empty;
}

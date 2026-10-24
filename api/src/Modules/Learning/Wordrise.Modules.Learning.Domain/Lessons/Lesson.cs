using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Domain.Lessons;

/// <summary>Which course a lesson belongs to.</summary>
public enum LessonTrack
{
    /// <summary>The main course: a dialogue, a grammar point and two exercises per lesson.</summary>
    Main,

    /// <summary>Before the main course: letters, numbers, greetings and the first words to read.</summary>
    Foundations,
}

/// <summary>
/// One daily lesson. In the main course it is built around a real-life scenario and one grammar point;
/// in the foundations course it introduces items (letters, numbers, words) and practises them.
/// Published by content authoring or seeded; read-only for learners.
/// </summary>
public sealed class Lesson
{
    private Lesson()
    {
    }

    public Guid Id { get; private set; }

    public LessonTrack Track { get; private set; } = LessonTrack.Main;

    /// <summary>Position in its course; learners take lessons in this order.</summary>
    public int Number { get; private set; }

    /// <summary>
    /// The step of the grammar curriculum this lesson teaches, when it comes from one. Lets a learner who
    /// already knows the basics start further on. Null for foundations and for lessons that predate it.
    /// </summary>
    public int? GrammarStep { get; private set; }

    /// <summary>ISO code of the language being taught, for example en.</summary>
    public string TargetLanguage { get; private set; } = LessonLanguages.English;

    /// <summary>ISO code of the language the lesson explains it in, for example ar.</summary>
    public string NativeLanguage { get; private set; } = LessonLanguages.Arabic;

    public string Title { get; private set; } = string.Empty;

    public string Scenario { get; private set; } = string.Empty;

    public string GrammarPoint { get; private set; } = string.Empty;

    public LessonContent Content { get; private set; } = new();

    /// <exception cref="BusinessRuleException">The lesson could not be taught: a learner would hit a broken screen.</exception>
    public static Lesson Create(
        Guid id,
        int number,
        LessonTrack track,
        int? grammarStep,
        string targetLanguage,
        string nativeLanguage,
        string title,
        string scenario,
        string grammarPoint,
        LessonContent content)
    {
        targetLanguage = (targetLanguage ?? string.Empty).Trim().ToLowerInvariant();
        nativeLanguage = (nativeLanguage ?? string.Empty).Trim().ToLowerInvariant();
        EnsureTeachable(track, grammarStep, targetLanguage, nativeLanguage, title, scenario, grammarPoint, content);
        return new Lesson
        {
            Id = id,
            Number = number,
            Track = track,
            GrammarStep = grammarStep,
            TargetLanguage = targetLanguage,
            NativeLanguage = nativeLanguage,
            Title = title.Trim(),
            Scenario = (scenario ?? string.Empty).Trim(),
            GrammarPoint = (grammarPoint ?? string.Empty).Trim(),
            Content = content,
        };
    }

    private static void EnsureTeachable(
        LessonTrack track,
        int? grammarStep,
        string targetLanguage,
        string nativeLanguage,
        string title,
        string scenario,
        string grammarPoint,
        LessonContent content)
    {
        var problem =
            !LessonLanguages.IsValidCode(targetLanguage) || !LessonLanguages.IsValidCode(nativeLanguage)
                ? "A lesson needs the language it teaches and the language it explains in (ISO codes such as en and ar)."
            : track == LessonTrack.Foundations
                ? FoundationsProblem(grammarStep, title, content)
                : MainProblem(grammarStep, title, scenario, grammarPoint, content);

        if (problem is not null)
        {
            throw new BusinessRuleException(LearningErrors.LessonInvalid, problem);
        }
    }

    private static string? MainProblem(int? grammarStep, string title, string scenario, string grammarPoint, LessonContent content) =>
        string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(scenario) || string.IsNullOrWhiteSpace(grammarPoint)
            ? "A lesson needs a title, a scenario and a grammar point."
        : grammarStep is < 1
            ? "The grammar step starts at 1."
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

    private static string? FoundationsProblem(int? grammarStep, string title, LessonContent content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "A foundations lesson needs a title.";
        }

        if (grammarStep is not null)
        {
            return "A foundations lesson does not belong to a grammar step.";
        }

        if (content.Words.Count == 0)
        {
            return "A foundations lesson needs at least one item.";
        }

        if (content.Words.Select(w => w.Id).Distinct().Count() != content.Words.Count)
        {
            return "An item appears twice in the same lesson.";
        }

        if (content.Words.Any(w => w.IsLetter && (w.Target.Length != 1 || string.IsNullOrWhiteSpace(w.Ipa))))
        {
            return "A letter is a single character with its sound written in IPA.";
        }

        if (content.Exercises.Count == 0)
        {
            return "A foundations lesson needs at least one exercise.";
        }

        foreach (var exercise in content.Exercises)
        {
            if (string.IsNullOrWhiteSpace(exercise.Prompt))
            {
                return "Every exercise needs an instruction.";
            }

            if (!HasExactlyOneCorrectAnswer(exercise.Options))
            {
                return $"The exercise \"{exercise.Prompt}\" needs at least two options and exactly one correct answer.";
            }

            if (exercise.AudioStyle == SpokenStyle.Sound && string.IsNullOrWhiteSpace(exercise.AudioIpa))
            {
                return $"The exercise \"{exercise.Prompt}\" plays a sound and needs its IPA spelling.";
            }

            if (string.IsNullOrWhiteSpace(exercise.Stimulus) && string.IsNullOrWhiteSpace(exercise.AudioText))
            {
                return $"The exercise \"{exercise.Prompt}\" shows or plays nothing.";
            }
        }

        return null;
    }

    private static bool HasExactlyOneCorrectAnswer(List<AnswerOption> options) =>
        options.Count >= 2 && options.Count(o => o.IsCorrect) == 1;
}

/// <summary>Everything the lesson screens show, in their order.</summary>
public sealed class LessonContent
{
    /// <summary>The words of a main lesson; the items (letters, numbers, words) of a foundations lesson.</summary>
    public List<LessonWord> Words { get; init; } = [];

    public List<DialogueLine> Dialogue { get; init; } = [];

    public string ChatPrompt { get; init; } = string.Empty;

    public List<AnswerOption> ChatOptions { get; init; } = [];

    public GrammarExplanation Grammar { get; init; } = new();

    public string ListeningSentence { get; init; } = string.Empty;

    public List<AnswerOption> ListeningOptions { get; init; } = [];

    /// <summary>The practice of a foundations lesson, in order. Empty in the main course.</summary>
    public List<FoundationExercise> Exercises { get; init; } = [];
}

public sealed class LessonWord
{
    public Guid Id { get; init; }

    /// <summary>What is spoken: the word, the letter's name ("A"), or the number in words ("seven").</summary>
    public string Target { get; init; } = string.Empty;

    public string Native { get; init; } = string.Empty;

    /// <summary>How the word sounds, written in the learner's own script.</summary>
    public string Pronunciation { get; init; } = string.Empty;

    public string Example { get; init; } = string.Empty;

    public string ExampleNative { get; init; } = string.Empty;

    /// <summary>What is shown big when it differs from <see cref="Target"/>: "Aa" for a letter, "7" for seven. Empty otherwise.</summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>A letter: its name is spoken by spelling it, and it has a sound.</summary>
    public bool IsLetter { get; init; }

    /// <summary>The letter's most common sound in the International Phonetic Alphabet. Letters only.</summary>
    public string Ipa { get; init; } = string.Empty;
}

/// <summary>How an exercise's audio is spoken.</summary>
public enum SpokenStyle
{
    Normal,

    /// <summary>The name of a letter.</summary>
    Letter,

    /// <summary>The sound of a letter.</summary>
    Sound,
}

/// <summary>One question of a foundations lesson: look at something and/or listen, then choose.</summary>
public sealed class FoundationExercise
{
    /// <summary>The instruction, in the learner's own language.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>What is shown big (a letter, a number, a word). Empty when the exercise is only listening.</summary>
    public string Stimulus { get; init; } = string.Empty;

    /// <summary>What is played when the learner taps the speaker. Empty when there is nothing to hear.</summary>
    public string AudioText { get; init; } = string.Empty;

    public SpokenStyle AudioStyle { get; init; } = SpokenStyle.Normal;

    /// <summary>The sound in IPA, when <see cref="AudioStyle"/> is <see cref="SpokenStyle.Sound"/>.</summary>
    public string AudioIpa { get; init; } = string.Empty;

    public List<AnswerOption> Options { get; init; } = [];
}

public sealed class DialogueLine
{
    public string Speaker { get; init; } = string.Empty;

    /// <summary>True when the learner says this line.</summary>
    public bool IsLearner { get; init; }

    public string Target { get; init; } = string.Empty;

    public string Native { get; init; } = string.Empty;
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
    public string Target { get; init; } = string.Empty;

    public string Native { get; init; } = string.Empty;
}

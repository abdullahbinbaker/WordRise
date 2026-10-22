using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <summary>What a lesson should teach: set by the content manager, followed by the writer.</summary>
public sealed class LessonBrief
{
    public const int MinWords = 3;
    public const int MaxWords = 10;
    public const int DefaultWords = 5;
    public const int MinScenarioLength = 2;
    public const int MaxScenarioLength = 60;

    private LessonBrief()
    {
    }

    /// <summary>ISO code of the language being taught.</summary>
    public string TargetLanguage { get; private set; } = SupportedCourses.English;

    /// <summary>ISO code of the language the lesson explains it in.</summary>
    public string NativeLanguage { get; private set; } = SupportedCourses.Arabic;

    /// <summary>The real-life situation, in the manager's words: "at the airport", "grocery store".</summary>
    public string Scenario { get; private set; } = string.Empty;

    /// <summary>The position in <see cref="GrammarCurriculum"/>.</summary>
    public int GrammarStep { get; private set; }

    /// <summary>How many new words the lesson introduces.</summary>
    public int WordCount { get; private set; }

    public GrammarPoint Grammar => GrammarCurriculum.Find(GrammarStep)!;

    public string Level => Grammar.Level;

    /// <summary>Titles of the grammar the lesson may use: its own point and every earlier one.</summary>
    public IReadOnlyList<string> AllowedGrammar => [.. GrammarCurriculum.UpTo(GrammarStep).Select(g => g.Title)];

    /// <exception cref="BusinessRuleException">The brief cannot produce a lesson.</exception>
    public static LessonBrief Create(
        string? scenario,
        int grammarStep,
        int? wordCount,
        string targetLanguage = SupportedCourses.English,
        string nativeLanguage = SupportedCourses.Arabic)
    {
        if (!SupportedCourses.IsSupported(targetLanguage, nativeLanguage))
        {
            throw Invalid($"There is no course that teaches '{targetLanguage}' to '{nativeLanguage}' speakers yet.");
        }

        var normalized = string.Join(' ', (scenario ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length is < MinScenarioLength or > MaxScenarioLength)
        {
            throw Invalid($"The scenario must be {MinScenarioLength} to {MaxScenarioLength} characters.");
        }

        if (GrammarCurriculum.Find(grammarStep) is null)
        {
            throw Invalid($"The grammar step must be 1 to {GrammarCurriculum.Steps.Count}.");
        }

        var words = wordCount ?? DefaultWords;
        if (words is < MinWords or > MaxWords)
        {
            throw Invalid($"A lesson introduces {MinWords} to {MaxWords} new words.");
        }

        return new LessonBrief
        {
            TargetLanguage = targetLanguage.ToLowerInvariant(),
            NativeLanguage = nativeLanguage.ToLowerInvariant(),
            Scenario = normalized,
            GrammarStep = grammarStep,
            WordCount = words,
        };
    }

    private static BusinessRuleException Invalid(string message) => new(AuthoringErrors.BriefInvalid, message);
}

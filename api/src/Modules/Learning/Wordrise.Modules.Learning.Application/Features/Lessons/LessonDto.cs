namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>A whole lesson, in the order its screens show it.</summary>
public sealed record LessonDto(
    Guid Id,
    int Number,
    string Track,
    int? GrammarStep,
    string TargetLanguage,
    string NativeLanguage,
    string Title,
    string Scenario,
    string GrammarPoint,
    IReadOnlyList<LessonWordDto> Words,
    IReadOnlyList<DialogueLineDto> Dialogue,
    string ChatPrompt,
    IReadOnlyList<AnswerOptionDto> ChatOptions,
    GrammarDto Grammar,
    ListeningDto Listening,
    IReadOnlyList<FoundationExerciseDto> Exercises);

public sealed record LessonWordDto(
    Guid Id,
    string Target,
    string Native,
    string Pronunciation,
    string Example,
    string ExampleNative,
    string Symbol,
    bool IsLetter);

public sealed record DialogueLineDto(string Speaker, bool IsLearner, string Target, string Native);

public sealed record AnswerOptionDto(string Text, bool IsCorrect);

public sealed record GrammarDto(
    string Title,
    string Meaning,
    string Explanation,
    string Pattern,
    IReadOnlyList<GrammarExampleDto> Examples,
    string Note);

public sealed record GrammarExampleDto(string Target, string Native);

public sealed record ListeningDto(string Sentence, IReadOnlyList<AnswerOptionDto> Options);

public sealed record CompleteLessonDto(bool AlreadyCompleted, int WordsAdded, int Streak);

/// <summary>One question of a foundations lesson.</summary>
/// <param name="AudioStyle">Empty for ordinary speech, "letter" for a letter's name, "sound" for its sound.</param>
public sealed record FoundationExerciseDto(
    string Prompt,
    string Stimulus,
    string AudioText,
    string AudioStyle,
    IReadOnlyList<AnswerOptionDto> Options);

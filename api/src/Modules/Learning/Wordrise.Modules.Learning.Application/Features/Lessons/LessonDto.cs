namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>A whole lesson, in the order its screens show it.</summary>
public sealed record LessonDto(
    Guid Id,
    int Number,
    string Title,
    string Scenario,
    string GrammarPoint,
    IReadOnlyList<LessonWordDto> Words,
    IReadOnlyList<DialogueLineDto> Dialogue,
    string ChatPrompt,
    IReadOnlyList<AnswerOptionDto> ChatOptions,
    GrammarDto Grammar,
    ListeningDto Listening);

public sealed record LessonWordDto(
    Guid Id,
    string English,
    string Arabic,
    string Pronunciation,
    string Example,
    string ExampleArabic);

public sealed record DialogueLineDto(string Speaker, bool IsLearner, string English, string Arabic);

public sealed record AnswerOptionDto(string Text, bool IsCorrect);

public sealed record GrammarDto(
    string Title,
    string Meaning,
    string Explanation,
    string Pattern,
    IReadOnlyList<GrammarExampleDto> Examples,
    string Note);

public sealed record GrammarExampleDto(string English, string Arabic);

public sealed record ListeningDto(string Sentence, IReadOnlyList<AnswerOptionDto> Options);

public sealed record CompleteLessonDto(bool AlreadyCompleted, int WordsAdded, int Streak);

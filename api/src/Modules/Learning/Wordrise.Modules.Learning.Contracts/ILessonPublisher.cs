namespace Wordrise.Modules.Learning.Contracts;

/// <summary>How other modules put a finished lesson in front of learners.</summary>
public interface ILessonPublisher
{
    /// <summary>
    /// Publishes the lesson as the next one in the course. Safe to repeat: a lesson id that is
    /// already published is left alone and its existing number is returned.
    /// </summary>
    /// <exception cref="Wordrise.Shared.Kernel.BusinessRuleException">The lesson breaks a Learning rule.</exception>
    Task<PublishedLessonInfo> PublishAsync(PublishLessonRequest request, CancellationToken cancellationToken);
}

/// <summary>What other modules may know about the vocabulary learners were taught.</summary>
public interface IPublishedVocabulary
{
    /// <summary>The words of every published lesson that teaches this language, without repeats.</summary>
    Task<IReadOnlyList<string>> GetTaughtWordsAsync(string targetLanguage, CancellationToken cancellationToken);
}

public sealed record PublishLessonRequest(
    Guid LessonId,
    string TargetLanguage,
    string NativeLanguage,
    string Title,
    string Scenario,
    string GrammarPoint,
    IReadOnlyList<PublishedWord> Words,
    IReadOnlyList<PublishedDialogueLine> Dialogue,
    string ChatPrompt,
    IReadOnlyList<PublishedAnswerOption> ChatOptions,
    PublishedGrammar Grammar,
    string ListeningSentence,
    IReadOnlyList<PublishedAnswerOption> ListeningOptions);

public sealed record PublishedWord(
    string Target,
    string Native,
    string Pronunciation,
    string Example,
    string ExampleNative);

public sealed record PublishedDialogueLine(string Speaker, bool IsLearner, string Target, string Native);

public sealed record PublishedAnswerOption(string Text, bool IsCorrect);

public sealed record PublishedGrammar(
    string Title,
    string Meaning,
    string Explanation,
    string Pattern,
    IReadOnlyList<PublishedGrammarExample> Examples,
    string Note);

public sealed record PublishedGrammarExample(string Target, string Native);

public sealed record PublishedLessonInfo(Guid LessonId, int Number, bool AlreadyPublished);

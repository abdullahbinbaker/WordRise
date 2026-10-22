using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Application.Abstractions;

/// <summary>Everything the writer and the reviewer are told about the lesson they work on.</summary>
/// <param name="KnownWords">English words earlier lessons already taught; the lesson should build on them.</param>
/// <param name="HumanFeedback">Notes a person left when sending the lesson back.</param>
public sealed record AuthoringContext(
    LessonBrief Brief,
    IReadOnlyList<string> KnownWords,
    IReadOnlyList<string> HumanFeedback);

/// <summary>Writes lessons. The provider behind it (an AI model, a sample generator) is replaceable.</summary>
public interface ILessonWriter
{
    Task<DraftContent> WriteAsync(AuthoringContext context, CancellationToken cancellationToken);

    /// <summary>Fixes the lesson using the verdict on it and any notes from a person.</summary>
    Task<DraftContent> ReviseAsync(
        AuthoringContext context,
        DraftContent previous,
        ReviewReport review,
        CancellationToken cancellationToken);
}

/// <summary>Reviews lessons. Deliberately a separate role (and model) from the writer: it hunts for mistakes.</summary>
public interface ILessonReviewer
{
    Task<ReviewReport> ReviewAsync(AuthoringContext context, DraftContent content, CancellationToken cancellationToken);
}

/// <summary>The words learners were already taught.</summary>
public interface IKnownVocabulary
{
    Task<IReadOnlyList<string>> GetTaughtWordsAsync(CancellationToken cancellationToken);
}

public sealed record PublishedLesson(Guid LessonId, int Number);

/// <summary>Puts a finished lesson in front of learners.</summary>
public interface ILessonPublisher
{
    /// <summary>Publishing the same draft twice is harmless: it returns the lesson already published.</summary>
    Task<Result<PublishedLesson>> PublishAsync(
        Guid draftId,
        LessonBrief brief,
        DraftContent content,
        CancellationToken cancellationToken);
}

/// <summary>The reviewer or admin making the request.</summary>
public interface ICurrentStaff
{
    Guid Id { get; }
}

/// <summary>Authoring rules whose values come from configuration.</summary>
public interface IAuthoringSettings
{
    /// <summary>Write-and-review attempts before a lesson goes to a person marked "needs attention".</summary>
    int MaxCycles { get; }
}

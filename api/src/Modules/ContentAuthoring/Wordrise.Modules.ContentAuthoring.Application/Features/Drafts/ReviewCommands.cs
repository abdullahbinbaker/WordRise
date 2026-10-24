using Mediator;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;

/// <summary>
/// A person approves the lesson: it is published to learners. Safe to repeat, so a double click
/// or a retry after a lost response never publishes twice.
/// </summary>
public sealed record ApproveDraftCommand(Guid DraftId) : ICommand<Result<PublishedLessonDto>>;

public sealed class ApproveDraftCommandHandler(
    IDraftRepository drafts,
    ILessonPublisher publisher,
    IUnitOfWork unitOfWork,
    ICurrentStaff staff,
    TimeProvider clock) : ICommandHandler<ApproveDraftCommand, Result<PublishedLessonDto>>
{
    public async ValueTask<Result<PublishedLessonDto>> Handle(ApproveDraftCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.GetAsync(command.DraftId, cancellationToken) is not { } draft)
        {
            return Error.NotFound(AuthoringErrors.DraftNotFound, "Draft not found.");
        }

        if (draft is { Status: DraftStatus.Published, PublishedLessonId: { } lessonId, PublishedLessonNumber: { } number })
        {
            return new PublishedLessonDto(lessonId, number);
        }

        draft.EnsureCanPublish();

        // Publish first, record after: the lesson id is the draft id, so if recording fails
        // the retry finds the lesson already published and just records it.
        var published = await publisher.PublishAsync(draft.Id, draft.Brief, draft.CurrentContent!, cancellationToken);
        if (!published.IsSuccess)
        {
            return published.Error!;
        }

        draft.MarkPublished(staff.Id, published.Value.LessonId, published.Value.Number, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PublishedLessonDto(published.Value.LessonId, published.Value.Number);
    }
}

/// <summary>A person sends the lesson back with a note; the writer revises it.</summary>
public sealed record RejectDraftCommand(Guid DraftId, string? Note) : ICommand<Result>;

public sealed class RejectDraftCommandHandler(
    IDraftRepository drafts,
    IUnitOfWork unitOfWork,
    ICurrentStaff staff,
    TimeProvider clock) : ICommandHandler<RejectDraftCommand, Result>
{
    public async ValueTask<Result> Handle(RejectDraftCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.GetAsync(command.DraftId, cancellationToken) is not { } draft)
        {
            return Error.NotFound(AuthoringErrors.DraftNotFound, "Draft not found.");
        }

        draft.RejectByHuman(command.Note, staff.Id, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>Puts a draft whose run broke back in the queue.</summary>
public sealed record RequeueDraftCommand(Guid DraftId) : ICommand<Result>;

public sealed class RequeueDraftCommandHandler(IDraftRepository drafts, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RequeueDraftCommand, Result>
{
    public async ValueTask<Result> Handle(RequeueDraftCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.GetAsync(command.DraftId, cancellationToken) is not { } draft)
        {
            return Error.NotFound(AuthoringErrors.DraftNotFound, "Draft not found.");
        }

        // A draft being worked on right now must not be requeued from under the writer.
        if (draft.Status != DraftStatus.Failed)
        {
            throw new BusinessRuleException(AuthoringErrors.InvalidState, $"Only a failed draft can be requeued; this one is {draft.Status}.");
        }

        draft.Requeue(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>Sets a lesson aside. It stays in the database and can be restored.</summary>
public sealed record DiscardDraftCommand(Guid DraftId) : ICommand<Result>;

public sealed class DiscardDraftCommandHandler(IDraftRepository drafts, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<DiscardDraftCommand, Result>
{
    public async ValueTask<Result> Handle(DiscardDraftCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.GetAsync(command.DraftId, cancellationToken) is not { } draft)
        {
            return Error.NotFound(AuthoringErrors.DraftNotFound, "Draft not found.");
        }

        draft.Discard(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

/// <summary>Brings a discarded lesson back to where it was.</summary>
public sealed record RestoreDraftCommand(Guid DraftId) : ICommand<Result>;

public sealed class RestoreDraftCommandHandler(IDraftRepository drafts, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RestoreDraftCommand, Result>
{
    public async ValueTask<Result> Handle(RestoreDraftCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.GetAsync(command.DraftId, cancellationToken) is not { } draft)
        {
            return Error.NotFound(AuthoringErrors.DraftNotFound, "Draft not found.");
        }

        draft.Restore(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

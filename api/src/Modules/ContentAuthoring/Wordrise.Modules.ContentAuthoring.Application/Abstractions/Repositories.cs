using Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Application.Abstractions;

// Write side: commands load drafts through these, change them, then save the unit of work.

public interface IDraftRepository
{
    Task<LessonDraft?> GetAsync(Guid draftId, CancellationToken cancellationToken);

    void Add(LessonDraft draft);

    /// <summary>Ids of drafts waiting for the writer, oldest first.</summary>
    Task<IReadOnlyList<Guid>> GetQueuedIdsAsync(int take, CancellationToken cancellationToken);

    /// <summary>Drafts whose run never finished, e.g. because the app restarted.</summary>
    Task<IReadOnlyList<LessonDraft>> GetInProgressAsync(CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Read side: queries return screen-ready data without going through the aggregate.</summary>
public interface IAuthoringQueries
{
    Task<IReadOnlyList<DraftListItemDto>> ListDraftsAsync(DraftStatus? status, CancellationToken cancellationToken);

    Task<DraftDetailDto?> GetDraftAsync(Guid draftId, CancellationToken cancellationToken);
}

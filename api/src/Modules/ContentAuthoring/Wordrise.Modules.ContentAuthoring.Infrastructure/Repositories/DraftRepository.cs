using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Contexts;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Repositories;

internal sealed class DraftRepository(AuthoringDbContext db) : IDraftRepository
{
    public Task<LessonDraft?> GetAsync(Guid draftId, CancellationToken cancellationToken) =>
        db.Drafts.SingleOrDefaultAsync(d => d.Id == draftId, cancellationToken);

    public void Add(LessonDraft draft) => db.Drafts.Add(draft);

    public async Task<IReadOnlyList<Guid>> GetQueuedIdsAsync(int take, CancellationToken cancellationToken) =>
        await db.Drafts
            .AsNoTracking()
            .Where(d => d.Status == DraftStatus.Queued)
            .OrderBy(d => d.CreatedAtUtc)
            .Take(take)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LessonDraft>> GetInProgressAsync(CancellationToken cancellationToken) =>
        await db.Drafts.Where(d => d.Status == DraftStatus.InProgress).ToListAsync(cancellationToken);
}

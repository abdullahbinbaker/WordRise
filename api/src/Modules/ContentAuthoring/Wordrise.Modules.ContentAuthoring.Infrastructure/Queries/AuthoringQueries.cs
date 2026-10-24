using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Contexts;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Queries;

/// <summary>Read side: no tracking, results shaped for the admin screens.</summary>
internal sealed class AuthoringQueries(AuthoringDbContext db) : IAuthoringQueries
{
    public async Task<IReadOnlyList<DraftListItemDto>> ListDraftsAsync(DraftStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Drafts.AsNoTracking();
        // Discarded lessons have their own tab; the default list is only what is still alive.
        query = status is { } wanted
            ? query.Where(d => d.Status == wanted)
            : query.Where(d => d.Status != DraftStatus.Discarded);

        // A review queue holds tens of drafts, so the mapping is done after loading them.
        var drafts = await query.OrderByDescending(d => d.UpdatedAtUtc).ToListAsync(cancellationToken);
        return [.. drafts.Select(DraftMapping.ToListItem)];
    }

    public async Task<IReadOnlyList<DraftCountDto>> CountDraftsAsync(CancellationToken cancellationToken) =>
        await db.Drafts.AsNoTracking()
            .GroupBy(d => d.Status)
            .Select(g => new DraftCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

    public async Task<DraftDetailDto?> GetDraftAsync(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await db.Drafts.AsNoTracking().SingleOrDefaultAsync(d => d.Id == draftId, cancellationToken);
        return draft is null ? null : DraftMapping.ToDetail(draft);
    }
}

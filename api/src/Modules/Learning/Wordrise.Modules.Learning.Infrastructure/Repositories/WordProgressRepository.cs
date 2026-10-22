using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Modules.Learning.Infrastructure.Contexts;

namespace Wordrise.Modules.Learning.Infrastructure.Repositories;

internal sealed class WordProgressRepository(LearningDbContext db) : IWordProgressRepository
{
    public Task<WordProgress?> GetAsync(Guid learnerId, Guid wordId, CancellationToken cancellationToken) =>
        db.WordProgress.SingleOrDefaultAsync(w => w.LearnerId == learnerId && w.WordId == wordId, cancellationToken);

    public Task<int> CountDueAsync(Guid learnerId, DateOnly today, CancellationToken cancellationToken) =>
        db.WordProgress.CountAsync(w => w.LearnerId == learnerId && w.DueOn <= today, cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetKnownWordIdsAsync(
        Guid learnerId,
        IReadOnlyCollection<Guid> wordIds,
        CancellationToken cancellationToken)
    {
        var known = await db.WordProgress
            .Where(w => w.LearnerId == learnerId && wordIds.Contains(w.WordId))
            .Select(w => w.WordId)
            .ToListAsync(cancellationToken);
        return known.ToHashSet();
    }

    public void AddRange(IEnumerable<WordProgress> words) => db.WordProgress.AddRange(words);
}

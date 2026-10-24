using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Learning.Infrastructure.Contexts;

namespace Wordrise.Modules.Learning.Infrastructure.Repositories;

internal sealed class LessonRepository(LearningDbContext db) : ILessonRepository
{
    public Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken) =>
        db.Lessons.AsNoTracking().SingleOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

    public async Task<IReadOnlyList<Lesson>> ListAsync(CancellationToken cancellationToken) =>
        await db.Lessons.AsNoTracking().OrderBy(l => l.Number).ToListAsync(cancellationToken);

    public async Task<int?> GetNumberAsync(Guid lessonId, CancellationToken cancellationToken) =>
        await db.Lessons
            .AsNoTracking()
            .Where(l => l.Id == lessonId)
            .Select(l => (int?)l.Number)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<int> NextNumberAsync(LessonTrack track, CancellationToken cancellationToken) =>
        (await db.Lessons.Where(l => l.Track == track).MaxAsync(l => (int?)l.Number, cancellationToken) ?? 0) + 1;

    public void Add(Lesson lesson) => db.Lessons.Add(lesson);
}

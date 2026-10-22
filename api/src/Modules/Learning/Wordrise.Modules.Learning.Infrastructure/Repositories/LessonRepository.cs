using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Learning.Infrastructure.Contexts;

namespace Wordrise.Modules.Learning.Infrastructure.Repositories;

internal sealed class LessonRepository(LearningDbContext db) : ILessonRepository
{
    public Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken) =>
        db.Lessons.AsNoTracking().SingleOrDefaultAsync(l => l.Id == lessonId, cancellationToken);
}

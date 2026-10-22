using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Modules.Learning.Infrastructure.Contexts;

namespace Wordrise.Modules.Learning.Infrastructure.Repositories;

internal sealed class LearnerRepository(LearningDbContext db) : ILearnerRepository
{
    public Task<Learner?> GetAsync(Guid learnerId, CancellationToken cancellationToken) =>
        db.Learners.SingleOrDefaultAsync(l => l.Id == learnerId, cancellationToken);

    public void Add(Learner learner) => db.Learners.Add(learner);
}

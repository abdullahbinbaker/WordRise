using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Application.Abstractions;

// Write side: commands load aggregates through these, change them, then save the unit of work.

public interface ILearnerRepository
{
    Task<Learner?> GetAsync(Guid learnerId, CancellationToken cancellationToken);

    void Add(Learner learner);
}

public interface IWordProgressRepository
{
    Task<WordProgress?> GetAsync(Guid learnerId, Guid wordId, CancellationToken cancellationToken);

    Task<int> CountDueAsync(Guid learnerId, DateOnly today, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetKnownWordIdsAsync(
        Guid learnerId,
        IReadOnlyCollection<Guid> wordIds,
        CancellationToken cancellationToken);

    void AddRange(IEnumerable<WordProgress> words);
}

public interface ILessonRepository
{
    Task<Lesson?> GetAsync(Guid lessonId, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

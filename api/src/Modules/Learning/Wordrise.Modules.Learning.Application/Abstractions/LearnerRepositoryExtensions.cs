using Wordrise.Modules.Learning.Domain.Learners;

namespace Wordrise.Modules.Learning.Application.Abstractions;

public static class LearnerRepositoryExtensions
{
    /// <summary>Anyone signed in is a learner: their learning record starts on first use.</summary>
    public static async Task<Learner> GetOrStartAsync(
        this ILearnerRepository learners,
        Guid learnerId,
        CancellationToken cancellationToken)
    {
        if (await learners.GetAsync(learnerId, cancellationToken) is { } existing)
        {
            return existing;
        }

        var learner = Learner.Start(learnerId);
        learners.Add(learner);
        return learner;
    }
}

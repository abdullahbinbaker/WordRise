using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Application.Abstractions;

/// <summary>Learning rules whose values come from configuration.</summary>
public interface ILearningSettings
{
    /// <summary>False: no reviews are due and lessons start with the new words.</summary>
    bool ReviewsEnabled { get; }

    int MaxReviewsPerSession { get; }

    /// <summary><see cref="ReviewBacklogPolicy.Off"/> when reviews or the backlog rule are switched off.</summary>
    ReviewBacklogPolicy ReviewBacklog { get; }
}

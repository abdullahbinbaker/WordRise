using Microsoft.Extensions.Options;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Infrastructure.Services;

internal sealed class LearningSettings(IOptions<LearningOptions> options) : ILearningSettings
{
    private readonly ReviewOptions _reviews = options.Value.Reviews;

    public bool ReviewsEnabled => _reviews.Enabled;

    public int MaxReviewsPerSession => _reviews.MaxPerSession;

    // Without reviews there is no backlog to pause for.
    public ReviewBacklogPolicy ReviewBacklog { get; } =
        options.Value.Reviews is { Enabled: true, Backlog.Enabled: true } reviews
            ? ReviewBacklogPolicy.WithLimit(reviews.Backlog.Limit)
            : ReviewBacklogPolicy.Off;
}

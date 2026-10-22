using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Reviews;

/// <summary>
/// The words to review in this session, oldest first, capped so a learner back after weeks
/// is not buried. Empty while reviews are switched off.
/// </summary>
public sealed record GetDueReviewsQuery : IQuery<Result<IReadOnlyList<DueReviewDto>>>;

public sealed class GetDueReviewsQueryHandler(
    ILearningQueries queries,
    ICurrentLearner learner,
    ILearningCalendar calendar,
    ILearningSettings settings) : IQueryHandler<GetDueReviewsQuery, Result<IReadOnlyList<DueReviewDto>>>
{
    public async ValueTask<Result<IReadOnlyList<DueReviewDto>>> Handle(
        GetDueReviewsQuery query,
        CancellationToken cancellationToken)
    {
        if (!settings.ReviewsEnabled)
        {
            return Result<IReadOnlyList<DueReviewDto>>.Success([]);
        }

        var due = await queries.GetDueReviewsAsync(
            learner.Id,
            calendar.Today,
            settings.MaxReviewsPerSession,
            cancellationToken);
        return Result<IReadOnlyList<DueReviewDto>>.Success(due);
    }
}

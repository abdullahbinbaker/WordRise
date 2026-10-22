using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Goal;

public sealed record GetDailyGoalQuery : IQuery<Result<DailyGoalDto>>;

public sealed class GetDailyGoalQueryHandler(ILearningQueries queries, ICurrentLearner learner)
    : IQueryHandler<GetDailyGoalQuery, Result<DailyGoalDto>>
{
    public async ValueTask<Result<DailyGoalDto>> Handle(GetDailyGoalQuery query, CancellationToken cancellationToken) =>
        await queries.GetDailyGoalAsync(learner.Id, cancellationToken);
}

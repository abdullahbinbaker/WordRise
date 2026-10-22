using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Today;

public sealed record GetTodayQuery : IQuery<Result<TodayDto>>;

public sealed class GetTodayQueryHandler(
    ILearningQueries queries,
    ICurrentLearner learner,
    ILearningCalendar calendar) : IQueryHandler<GetTodayQuery, Result<TodayDto>>
{
    public async ValueTask<Result<TodayDto>> Handle(GetTodayQuery query, CancellationToken cancellationToken) =>
        await queries.GetTodayAsync(learner.Id, calendar.Today, cancellationToken);
}

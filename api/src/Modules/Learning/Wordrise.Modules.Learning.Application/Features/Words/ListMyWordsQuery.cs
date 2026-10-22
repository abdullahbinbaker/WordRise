using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Words;

public sealed record ListMyWordsQuery(WordFilter Filter) : IQuery<Result<IReadOnlyList<MyWordDto>>>;

public sealed class ListMyWordsQueryHandler(
    ILearningQueries queries,
    ICurrentLearner learner,
    ILearningCalendar calendar) : IQueryHandler<ListMyWordsQuery, Result<IReadOnlyList<MyWordDto>>>
{
    public async ValueTask<Result<IReadOnlyList<MyWordDto>>> Handle(
        ListMyWordsQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<MyWordDto>>.Success(
            await queries.ListWordsAsync(learner.Id, query.Filter, calendar.Today, cancellationToken));
}

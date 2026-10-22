using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Words;

/// <summary>The English words of every published lesson, without repeats.</summary>
public sealed record GetTaughtWordsQuery : IQuery<Result<IReadOnlyList<string>>>;

public sealed class GetTaughtWordsQueryHandler(ILearningQueries queries)
    : IQueryHandler<GetTaughtWordsQuery, Result<IReadOnlyList<string>>>
{
    public async ValueTask<Result<IReadOnlyList<string>>> Handle(
        GetTaughtWordsQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<string>>.Success(await queries.GetTaughtWordsAsync(cancellationToken));
}

using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Words;

/// <summary>The words of every published lesson that teaches the language, without repeats.</summary>
public sealed record GetTaughtWordsQuery(string TargetLanguage) : IQuery<Result<IReadOnlyList<string>>>;

public sealed class GetTaughtWordsQueryHandler(ILearningQueries queries)
    : IQueryHandler<GetTaughtWordsQuery, Result<IReadOnlyList<string>>>
{
    public async ValueTask<Result<IReadOnlyList<string>>> Handle(
        GetTaughtWordsQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<string>>.Success(await queries.GetTaughtWordsAsync(query.TargetLanguage, cancellationToken));
}

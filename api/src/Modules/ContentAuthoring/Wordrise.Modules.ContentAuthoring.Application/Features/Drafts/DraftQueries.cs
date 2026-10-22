using Mediator;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;

/// <summary>The drafts, most recently changed first, optionally only those in one state.</summary>
public sealed record ListDraftsQuery(DraftStatus? Status) : IQuery<Result<IReadOnlyList<DraftListItemDto>>>;

public sealed class ListDraftsQueryHandler(IAuthoringQueries queries)
    : IQueryHandler<ListDraftsQuery, Result<IReadOnlyList<DraftListItemDto>>>
{
    public async ValueTask<Result<IReadOnlyList<DraftListItemDto>>> Handle(
        ListDraftsQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<DraftListItemDto>>.Success(await queries.ListDraftsAsync(query.Status, cancellationToken));
}

/// <summary>One draft with everything a reviewer needs: the lesson, every version, every verdict.</summary>
public sealed record GetDraftQuery(Guid DraftId) : IQuery<Result<DraftDetailDto>>;

public sealed class GetDraftQueryHandler(IAuthoringQueries queries) : IQueryHandler<GetDraftQuery, Result<DraftDetailDto>>
{
    public async ValueTask<Result<DraftDetailDto>> Handle(GetDraftQuery query, CancellationToken cancellationToken) =>
        await queries.GetDraftAsync(query.DraftId, cancellationToken) is { } draft
            ? draft
            : Error.NotFound(AuthoringErrors.DraftNotFound, "Draft not found.");
}

/// <summary>The grammar sequence a lesson is built on, for the "new lesson" form.</summary>
public sealed record GetCurriculumQuery : IQuery<Result<IReadOnlyList<GrammarPointDto>>>;

public sealed class GetCurriculumQueryHandler : IQueryHandler<GetCurriculumQuery, Result<IReadOnlyList<GrammarPointDto>>>
{
    public ValueTask<Result<IReadOnlyList<GrammarPointDto>>> Handle(
        GetCurriculumQuery query,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result<IReadOnlyList<GrammarPointDto>>.Success(
            [.. GrammarCurriculum.Steps.Select(s => new GrammarPointDto(s.Number, s.Title, s.Level))]));
}

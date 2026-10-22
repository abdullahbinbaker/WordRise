using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.ContentAuthoring.Api.Controllers;

/// <summary>
/// The review queue: lessons the AI wrote, waiting for a person. Reviewers and admins only,
/// and only when they signed in with two-factor authentication.
/// </summary>
[ApiController]
[Route("api/v1/admin/lessons/drafts")]
[Authorize(Policy = Policies.Staff)]
[Tags("Authoring")]
internal sealed class DraftsController(IMediator mediator) : ControllerBase
{
    /// <summary>The drafts, most recently changed first. Filter with ?status=aiApproved, needsAttention, queued…</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DraftListItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] DraftStatus? status, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new ListDraftsQuery(status), cancellationToken));

    /// <summary>The lesson as it stands, every version of it, and every verdict.</summary>
    [HttpGet("{draftId:guid}")]
    [ProducesResponseType<DraftDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid draftId, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetDraftQuery(draftId), cancellationToken));

    /// <summary>Asks for a new lesson. The writer picks it up from the queue; check back with GET.</summary>
    [HttpPost]
    [ProducesResponseType<CreatedDraft>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateDraftCommand(request.Scenario, request.GrammarStep, request.WordCount),
            cancellationToken);

        return result.IsSuccess
            ? Created($"/api/v1/admin/lessons/drafts/{result.Value}", new CreatedDraft(result.Value))
            : this.ToActionResult(result);
    }

    /// <summary>Publishes the lesson to learners. Safe to repeat.</summary>
    [HttpPost("{draftId:guid}/approve")]
    [ProducesResponseType<PublishedLessonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid draftId, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new ApproveDraftCommand(draftId), cancellationToken));

    /// <summary>Sends the lesson back to the writer with a note saying what to change.</summary>
    [HttpPost("{draftId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid draftId, RejectDraftRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new RejectDraftCommand(draftId, request.Note), cancellationToken));

    /// <summary>Puts a draft whose run broke (an outage, a bad answer) back in the queue.</summary>
    [HttpPost("{draftId:guid}/requeue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Requeue(Guid draftId, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new RequeueDraftCommand(draftId), cancellationToken));
}

internal sealed record CreateDraftRequest(string? Scenario, int GrammarStep, int? WordCount);

internal sealed record RejectDraftRequest(string? Note);

internal sealed record CreatedDraft(Guid Id);

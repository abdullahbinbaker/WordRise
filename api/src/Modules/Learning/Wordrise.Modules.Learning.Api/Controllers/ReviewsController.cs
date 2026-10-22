using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Learning.Application.Features.Reviews;
using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api.Controllers;

/// <summary>Spaced-repetition reviews of the words the learner has met.</summary>
[ApiController]
[Route("api/v1/learning/reviews")]
[Authorize]
[Tags("Learning")]
internal sealed class ReviewsController(IMediator mediator) : ControllerBase
{
    [HttpGet("due")]
    [ProducesResponseType<IReadOnlyList<DueReviewDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDue(CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetDueReviewsQuery(), cancellationToken));

    /// <summary>Records how hard the word was: hard, good or easy.</summary>
    [HttpPost("{wordId:guid}")]
    [ProducesResponseType<RateWordDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rate(Guid wordId, RateWordRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new RateWordCommand(wordId, request.Rating), cancellationToken));
}

internal sealed record RateWordRequest(ReviewRating Rating);

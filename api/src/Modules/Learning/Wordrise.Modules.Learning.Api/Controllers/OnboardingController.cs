using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Learning.Application.Features.Onboarding;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api.Controllers;

/// <summary>What the learner says at sign-up: do they know the letters, and where should the course start.</summary>
[ApiController]
[Route("api/v1/learning/onboarding")]
[Authorize]
[Tags("Learning")]
internal sealed class OnboardingController(IMediator mediator) : ControllerBase
{
    /// <summary>The current answers and the starting points to choose from.</summary>
    [HttpGet]
    [ProducesResponseType<OnboardingDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetOnboardingQuery(), cancellationToken));

    /// <summary>Saves the answers. The learner may change them later.</summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Set(SetOnboardingRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new SetOnboardingCommand(request.KnowsLetters, request.StartStep), cancellationToken));
}

internal sealed record SetOnboardingRequest(bool KnowsLetters, int StartStep);

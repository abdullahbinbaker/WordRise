using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Learning.Application.Features.Today;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api.Controllers;

/// <summary>The home screen: today's lesson, reviews due and the streak.</summary>
[ApiController]
[Route("api/v1/learning/today")]
[Authorize]
[Tags("Learning")]
internal sealed class TodayController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TodayDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetTodayQuery(), cancellationToken));
}

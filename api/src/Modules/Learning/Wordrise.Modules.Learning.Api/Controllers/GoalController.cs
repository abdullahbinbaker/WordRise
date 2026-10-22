using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Learning.Application.Features.Goal;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api.Controllers;

/// <summary>The learner's daily word commitment.</summary>
[ApiController]
[Route("api/v1/learning/goal")]
[Authorize]
[Tags("Learning")]
internal sealed class GoalController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DailyGoalDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetDailyGoalQuery(), cancellationToken));

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Set(SetDailyGoalRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new SetDailyGoalCommand(request.WordsPerDay), cancellationToken));
}

internal sealed record SetDailyGoalRequest(int WordsPerDay);

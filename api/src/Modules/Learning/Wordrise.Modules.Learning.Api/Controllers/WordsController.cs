using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Learning.Application.Features.Words;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api.Controllers;

/// <summary>The "My words" screen.</summary>
[ApiController]
[Route("api/v1/learning/words")]
[Authorize]
[Tags("Learning")]
internal sealed class WordsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MyWordDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] WordFilter filter, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new ListMyWordsQuery(filter), cancellationToken));
}

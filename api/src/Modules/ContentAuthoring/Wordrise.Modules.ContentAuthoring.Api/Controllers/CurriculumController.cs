using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.ContentAuthoring.Api.Controllers;

[ApiController]
[Route("api/v1/admin/lessons/curriculum")]
[Authorize(Policy = Policies.Staff)]
[Tags("Authoring")]
internal sealed class CurriculumController(IMediator mediator) : ControllerBase
{
    /// <summary>The grammar sequence lessons are built on: the choices for a new lesson's grammar step.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GrammarPointDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetCurriculumQuery(), cancellationToken));
}

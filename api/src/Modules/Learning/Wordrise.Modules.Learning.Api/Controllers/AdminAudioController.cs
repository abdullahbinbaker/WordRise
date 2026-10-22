using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Modules.Learning.Application.Features.Lessons;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Learning.Api.Controllers;

[ApiController]
[Route("api/v1/admin/lessons/audio")]
[Authorize(Policy = Policies.Staff)]
[Tags("Learning")]
internal sealed class AdminAudioController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Records the audio of every published lesson that lacks some, for lessons published before the
    /// speech provider was set up. Safe to repeat: finished recordings are skipped.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AudioGenerationResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Generate(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GenerateLessonAudioCommand(), cancellationToken));
}

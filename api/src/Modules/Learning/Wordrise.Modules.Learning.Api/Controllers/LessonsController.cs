using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Learning.Application.Features.Lessons;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Learning.Api.Controllers;

[ApiController]
[Route("api/v1/learning/lessons")]
[Authorize]
[Tags("Learning")]
internal sealed class LessonsController(IMediator mediator) : ControllerBase
{
    /// <summary>Everything the lesson screens need: words, dialogue, grammar and the exercises.</summary>
    [HttpGet("{lessonId:guid}")]
    [ProducesResponseType<LessonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid lessonId, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new GetLessonQuery(lessonId), cancellationToken));

    /// <summary>Marks the lesson done and schedules its words for review. Safe to call twice.</summary>
    [HttpPost("{lessonId:guid}/complete")]
    [ProducesResponseType<CompleteLessonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(Guid lessonId, CancellationToken cancellationToken) =>
        this.ToActionResult(await mediator.Send(new CompleteLessonCommand(lessonId), cancellationToken));
}

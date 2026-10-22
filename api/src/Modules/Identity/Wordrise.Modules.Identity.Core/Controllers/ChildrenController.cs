using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Identity.Core.Domain;
using Wordrise.Modules.Identity.Core.Profiles;

namespace Wordrise.Modules.Identity.Core.Controllers;

/// <summary>Child profiles managed by the signed-in parent.</summary>
[ApiController]
[Route("api/v1/me/children")]
[Authorize]
[Tags("Me")]
internal sealed class ChildrenController(ChildProfileService children, UserManager<AppUser> users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ChildProfileResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChildProfileResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await children.ListAsync(ParentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType<ChildProfileResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChildProfileResponse>> Create(
        ChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ProfileValidator.ValidateChild(request, children.CurrentYear);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        if (await users.GetUserAsync(User) is not { } parent)
        {
            return Unauthorized();
        }

        if (await children.CountAsync(parent.Id, cancellationToken) >= ProfileRules.MaxChildrenPerParent)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["children"] = [$"An account can have up to {ProfileRules.MaxChildrenPerParent} child profiles."],
            }));
        }

        var child = await children.CreateAsync(parent, request, cancellationToken);
        return Created($"/api/v1/me/children/{child.Id}", child);
    }

    [HttpPut("{childId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid childId,
        ChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var errors = ProfileValidator.ValidateChild(request, children.CurrentYear);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var updated = await children.UpdateAsync(ParentId, childId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{childId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid childId, CancellationToken cancellationToken)
    {
        if (await users.GetUserAsync(User) is not { } parent)
        {
            return Unauthorized();
        }

        var deleted = await children.DeleteAsync(parent, childId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>[Authorize] guarantees the user id claim is present.</summary>
    private Guid ParentId => Guid.Parse(users.GetUserId(User)!);
}

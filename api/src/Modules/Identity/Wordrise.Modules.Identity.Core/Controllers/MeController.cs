using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Identity.Core.Domain;
using Wordrise.Modules.Identity.Core.Profiles;

namespace Wordrise.Modules.Identity.Core.Controllers;

/// <summary>The signed-in account.</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize]
[Tags("Me")]
internal sealed class MeController(UserManager<AppUser> users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Get()
    {
        if (await users.GetUserAsync(User) is not { } user)
        {
            return Unauthorized();
        }

        var roles = await users.GetRolesAsync(user);
        return new MeResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            [.. roles.Order()],
            user.TwoFactorEnabled,
            User.HasClaim("amr", "mfa"));
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(UpdateMeRequest request)
    {
        var errors = ProfileValidator.ValidateDisplayName(request.DisplayName);
        if (errors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        if (await users.GetUserAsync(User) is not { } user)
        {
            return Unauthorized();
        }

        user.DisplayName = request.DisplayName!.Trim();
        await users.UpdateAsync(user);
        return NoContent();
    }
}

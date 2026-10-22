using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Identity.Core.Domain;

namespace Wordrise.Modules.Identity.Core.Controllers;

/// <summary>
/// Sign-out for the website. Register, login and refresh come ready-made from
/// <c>MapIdentityApi</c> under the same <c>/api/v1/auth</c> path.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Tags("Auth")]
internal sealed class AuthController(SignInManager<AppUser> signInManager) : ControllerBase
{
    /// <summary>Ends the cookie session. Mobile bearer tokens are simply discarded by the app.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}

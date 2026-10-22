using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Modules.Identity.Core.Domain;
using Wordrise.Modules.Identity.Core.Profiles;

namespace Wordrise.Modules.Identity.Core.Controllers;

/// <summary>
/// Who may review lessons. Admins (two-factor only) name reviewers among accounts that already
/// exist; the Admin role itself is granted in settings, never through the API.
/// </summary>
[ApiController]
[Route("api/v1/admin/staff")]
[Authorize(Policy = Policies.Admin)]
[Tags("Staff")]
internal sealed class StaffController(UserManager<AppUser> users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StaffMemberResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var staff = new Dictionary<Guid, AppUser>();
        foreach (var role in new[] { Roles.Admin, Roles.Reviewer })
        {
            foreach (var user in await users.GetUsersInRoleAsync(role))
            {
                staff[user.Id] = user;
            }
        }

        var result = new List<StaffMemberResponse>();
        foreach (var user in staff.Values.OrderBy(u => u.Email))
        {
            var roles = await users.GetRolesAsync(user);
            result.Add(new StaffMemberResponse(user.Id, user.Email!, user.DisplayName, [.. roles.Order()], user.TwoFactorEnabled));
        }

        return Ok(result);
    }

    /// <summary>Makes an existing account a reviewer. Safe to repeat.</summary>
    [HttpPost("reviewers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantReviewer(GrantReviewerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || await users.FindByEmailAsync(request.Email.Trim()) is not { } user)
        {
            return NotFound(new ProblemDetails { Title = "No account with this email. Ask them to register first." });
        }

        if (!await users.IsInRoleAsync(user, Roles.Reviewer))
        {
            await users.AddToRoleAsync(user, Roles.Reviewer);
        }

        return NoContent();
    }

    /// <summary>Takes the reviewer role away. Admins cannot be removed here.</summary>
    [HttpDelete("reviewers/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeReviewer(Guid userId)
    {
        if (await users.FindByIdAsync(userId.ToString()) is not { } user || !await users.IsInRoleAsync(user, Roles.Reviewer))
        {
            return NotFound();
        }

        await users.RemoveFromRoleAsync(user, Roles.Reviewer);
        return NoContent();
    }
}

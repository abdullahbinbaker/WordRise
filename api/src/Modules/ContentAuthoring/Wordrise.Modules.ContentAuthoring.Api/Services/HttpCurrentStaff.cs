using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;

namespace Wordrise.Modules.ContentAuthoring.Api.Services;

/// <summary>The staff member is the signed-in account: its id comes from the standard user id claim.</summary>
internal sealed class HttpCurrentStaff(IHttpContextAccessor httpContextAccessor) : ICurrentStaff
{
    public Guid Id =>
        Guid.Parse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authoring endpoints require a signed-in staff member."));
}

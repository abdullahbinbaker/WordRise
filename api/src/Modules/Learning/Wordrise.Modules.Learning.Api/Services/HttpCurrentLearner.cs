using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wordrise.Modules.Learning.Application.Abstractions;

namespace Wordrise.Modules.Learning.Api.Services;

/// <summary>The learner is the signed-in account: its id comes from the standard user id claim.</summary>
internal sealed class HttpCurrentLearner(IHttpContextAccessor httpContextAccessor) : ICurrentLearner
{
    public Guid Id =>
        Guid.Parse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Learning endpoints require a signed-in learner."));
}

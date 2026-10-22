using Microsoft.AspNetCore.Identity;

namespace Wordrise.Modules.Identity.Core.Domain;

internal sealed class AppUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}

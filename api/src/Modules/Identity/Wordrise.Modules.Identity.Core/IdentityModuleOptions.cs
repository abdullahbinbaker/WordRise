namespace Wordrise.Modules.Identity.Core;

internal sealed class IdentityModuleOptions
{
    public const string SectionName = "Identity";

    /// <summary>Sign-in, registration and password requests allowed per client per minute.</summary>
    public int AuthRequestsPerMinute { get; set; } = 10;
}

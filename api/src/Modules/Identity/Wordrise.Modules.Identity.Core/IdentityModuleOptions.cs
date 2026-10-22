namespace Wordrise.Modules.Identity.Core;

internal sealed class IdentityModuleOptions
{
    public const string SectionName = "Identity";

    /// <summary>Sign-in, registration and password requests allowed per client per minute.</summary>
    public int AuthRequestsPerMinute { get; set; } = 10;

    /// <summary>
    /// Accounts that receive the Admin role at startup. They must already be registered, and they still
    /// need two-factor sign-in to use it. Leave empty once the first admin exists.
    /// </summary>
    public string[] AdminEmails { get; set; } = [];
}

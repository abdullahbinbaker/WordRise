namespace Wordrise.Modules.Identity.Core;

internal sealed class IdentityModuleOptions
{
    public const string SectionName = "Identity";

    /// <summary>Sign-in, registration and password requests allowed per client per minute.</summary>
    public int AuthRequestsPerMinute { get; set; } = 10;

    /// <summary>
    /// A new account must open the link emailed to it before it can sign in. Switch off only for local runs
    /// where no email is delivered; it also stops anyone from claiming an admin address they do not own.
    /// </summary>
    public bool RequireConfirmedEmail { get; set; } = true;

    /// <summary>
    /// The website address used in emailed links, for example https://wordrise.example. Empty uses the
    /// address the API was reached on, which is right when the website and API share a domain.
    /// </summary>
    public string WebBaseUrl { get; set; } = "";

    /// <summary>
    /// Accounts that receive the Admin role at startup. They must already be registered, and they still
    /// need two-factor sign-in to use it. Leave empty once the first admin exists.
    /// </summary>
    public string[] AdminEmails { get; set; } = [];
}

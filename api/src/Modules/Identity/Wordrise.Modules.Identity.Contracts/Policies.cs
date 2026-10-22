namespace Wordrise.Modules.Identity.Contracts;

/// <summary>Authorization policies other modules can require on their endpoints.</summary>
public static class Policies
{
    /// <summary>Reviewers and admins who signed in with two-factor authentication.</summary>
    public const string Staff = "Staff";

    /// <summary>Admins who signed in with two-factor authentication: the only ones who manage staff.</summary>
    public const string Admin = "Admin";
}

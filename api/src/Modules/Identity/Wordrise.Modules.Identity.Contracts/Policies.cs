namespace Wordrise.Modules.Identity.Contracts;

/// <summary>Authorization policies other modules can require on their endpoints.</summary>
public static class Policies
{
    /// <summary>Reviewers and admins who signed in with two-factor authentication.</summary>
    public const string Staff = "Staff";
}

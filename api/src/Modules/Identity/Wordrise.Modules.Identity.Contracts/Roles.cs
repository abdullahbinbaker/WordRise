namespace Wordrise.Modules.Identity.Contracts;

/// <summary>
/// Every signed-in account is a learner; these roles are added on top of that.
/// </summary>
public static class Roles
{
    /// <summary>Granted automatically when the account adds its first child profile.</summary>
    public const string Parent = "Parent";

    public const string Reviewer = "Reviewer";

    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Parent, Reviewer, Admin];
}

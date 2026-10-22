namespace Wordrise.Modules.Identity.Core;

internal static class ProfileRules
{
    public const int MaxNameLength = 40;
    public const int MaxChildrenPerParent = 10;
    public const int MinChildAge = 3;
    public const int MaxChildAge = 17;
    public const int MinDailyMinutes = 5;
    public const int MaxDailyMinutes = 120;
    public const int DefaultDailyMinutes = 20;

    /// <summary>The avatars a child can pick on the profile screen.</summary>
    public static readonly IReadOnlySet<string> AvatarKeys =
        new HashSet<string>(["lion", "owl", "fox", "bear", "rabbit", "cat", "panda", "turtle"]);
}

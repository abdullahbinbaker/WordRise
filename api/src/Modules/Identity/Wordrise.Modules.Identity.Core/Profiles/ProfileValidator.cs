namespace Wordrise.Modules.Identity.Core.Profiles;

/// <summary>Returns field errors keyed like the request, ready for a validation problem response.</summary>
internal static class ProfileValidator
{
    public static Dictionary<string, string[]> ValidateDisplayName(string? displayName)
    {
        var errors = new Dictionary<string, string[]>();
        AddNameErrors(errors, displayName);
        return errors;
    }

    public static Dictionary<string, string[]> ValidateChild(ChildProfileRequest request, int currentYear)
    {
        var errors = new Dictionary<string, string[]>();
        AddNameErrors(errors, request.DisplayName);

        if (request.AvatarKey is null || !ProfileRules.AvatarKeys.Contains(request.AvatarKey))
        {
            errors["avatarKey"] = [$"Choose one of: {string.Join(", ", ProfileRules.AvatarKeys)}."];
        }

        var age = currentYear - request.BirthYear;
        if (age is < ProfileRules.MinChildAge or > ProfileRules.MaxChildAge)
        {
            errors["birthYear"] = [$"Child profiles are for ages {ProfileRules.MinChildAge} to {ProfileRules.MaxChildAge}."];
        }

        if (request.DailyMinutesLimit is { } minutes
            && minutes is < ProfileRules.MinDailyMinutes or > ProfileRules.MaxDailyMinutes)
        {
            errors["dailyMinutesLimit"] =
                [$"Daily limit must be {ProfileRules.MinDailyMinutes} to {ProfileRules.MaxDailyMinutes} minutes."];
        }

        return errors;
    }

    private static void AddNameErrors(Dictionary<string, string[]> errors, string? displayName)
    {
        var length = displayName?.Trim().Length ?? 0;
        if (length is 0 or > ProfileRules.MaxNameLength)
        {
            errors["displayName"] = [$"Name must be 1 to {ProfileRules.MaxNameLength} characters."];
        }
    }
}

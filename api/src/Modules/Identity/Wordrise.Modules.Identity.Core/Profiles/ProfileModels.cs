namespace Wordrise.Modules.Identity.Core.Profiles;

internal sealed record MeResponse(Guid Id, string Email, string? DisplayName, IReadOnlyList<string> Roles);

internal sealed record UpdateMeRequest(string? DisplayName);

internal sealed record ChildProfileRequest(string? DisplayName, string? AvatarKey, int BirthYear, int? DailyMinutesLimit);

internal sealed record ChildProfileResponse(
    Guid Id,
    string DisplayName,
    string AvatarKey,
    int BirthYear,
    int DailyMinutesLimit);

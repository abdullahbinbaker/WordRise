namespace Wordrise.Modules.Identity.Core.Profiles;

internal sealed record MeResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    bool TwoFactorEnabled,
    bool SignedInWithTwoFactor);

internal sealed record StaffMemberResponse(Guid Id, string Email, string? DisplayName, IReadOnlyList<string> Roles, bool TwoFactorEnabled);

internal sealed record GrantReviewerRequest(string? Email);

internal sealed record UpdateMeRequest(string? DisplayName);

internal sealed record ChildProfileRequest(string? DisplayName, string? AvatarKey, int BirthYear, int? DailyMinutesLimit);

internal sealed record ChildProfileResponse(
    Guid Id,
    string DisplayName,
    string AvatarKey,
    int BirthYear,
    int DailyMinutesLimit);

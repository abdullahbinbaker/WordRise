namespace Wordrise.Modules.Identity.Core.Domain;

/// <summary>
/// A child learner who has no credentials of their own: they pick their avatar on the
/// parent's signed-in device.
/// </summary>
internal sealed class ChildProfile
{
    public Guid Id { get; set; }

    public Guid ParentId { get; set; }

    public required string DisplayName { get; set; }

    public required string AvatarKey { get; set; }

    public int BirthYear { get; set; }

    public int DailyMinutesLimit { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

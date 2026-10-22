using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Modules.Identity.Core.Data;
using Wordrise.Modules.Identity.Core.Domain;

namespace Wordrise.Modules.Identity.Core.Profiles;

/// <summary>Child profiles belong to one parent; every query is scoped to that parent.</summary>
internal sealed class ChildProfileService(
    IdentityModuleDbContext db,
    UserManager<AppUser> users,
    TimeProvider clock)
{
    public int CurrentYear => clock.GetUtcNow().Year;

    public async Task<IReadOnlyList<ChildProfileResponse>> ListAsync(Guid parentId, CancellationToken cancellationToken) =>
        await db.ChildProfiles
            .AsNoTracking()
            .Where(c => c.ParentId == parentId)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new ChildProfileResponse(c.Id, c.DisplayName, c.AvatarKey, c.BirthYear, c.DailyMinutesLimit))
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(Guid parentId, CancellationToken cancellationToken) =>
        db.ChildProfiles.CountAsync(c => c.ParentId == parentId, cancellationToken);

    public async Task<ChildProfileResponse> CreateAsync(
        AppUser parent,
        ChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var child = new ChildProfile
        {
            Id = Guid.CreateVersion7(),
            ParentId = parent.Id,
            DisplayName = request.DisplayName!.Trim(),
            AvatarKey = request.AvatarKey!,
            BirthYear = request.BirthYear,
            DailyMinutesLimit = request.DailyMinutesLimit ?? ProfileRules.DefaultDailyMinutes,
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
        };
        db.ChildProfiles.Add(child);
        await db.SaveChangesAsync(cancellationToken);

        if (!await users.IsInRoleAsync(parent, Roles.Parent))
        {
            await users.AddToRoleAsync(parent, Roles.Parent);
        }

        return new ChildProfileResponse(child.Id, child.DisplayName, child.AvatarKey, child.BirthYear, child.DailyMinutesLimit);
    }

    /// <returns>False when the child does not exist or belongs to another parent.</returns>
    public async Task<bool> UpdateAsync(
        Guid parentId,
        Guid childId,
        ChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var child = await db.ChildProfiles.SingleOrDefaultAsync(
            c => c.Id == childId && c.ParentId == parentId,
            cancellationToken);
        if (child is null)
        {
            return false;
        }

        child.DisplayName = request.DisplayName!.Trim();
        child.AvatarKey = request.AvatarKey!;
        child.BirthYear = request.BirthYear;
        child.DailyMinutesLimit = request.DailyMinutesLimit ?? child.DailyMinutesLimit;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <returns>False when the child does not exist or belongs to another parent.</returns>
    public async Task<bool> DeleteAsync(AppUser parent, Guid childId, CancellationToken cancellationToken)
    {
        var deleted = await db.ChildProfiles
            .Where(c => c.Id == childId && c.ParentId == parent.Id)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return false;
        }

        if (await CountAsync(parent.Id, cancellationToken) == 0)
        {
            await users.RemoveFromRoleAsync(parent, Roles.Parent);
        }

        return true;
    }
}

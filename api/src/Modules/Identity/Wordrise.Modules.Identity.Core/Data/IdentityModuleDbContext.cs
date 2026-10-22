using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Identity.Core.Domain;

namespace Wordrise.Modules.Identity.Core.Data;

internal sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "identity";

    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<AppUser>(user => user.Property(u => u.DisplayName).HasMaxLength(ProfileRules.MaxNameLength));

        builder.Entity<ChildProfile>(child =>
        {
            child.ToTable("ChildProfiles");
            child.HasKey(c => c.Id);
            child.Property(c => c.DisplayName).HasMaxLength(ProfileRules.MaxNameLength).IsRequired();
            child.Property(c => c.AvatarKey).HasMaxLength(32).IsRequired();
            child.HasIndex(c => c.ParentId);
            child.HasOne<AppUser>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

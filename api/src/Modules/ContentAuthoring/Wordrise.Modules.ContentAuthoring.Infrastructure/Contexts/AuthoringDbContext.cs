using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Contexts;

internal sealed class AuthoringDbContext(DbContextOptions<AuthoringDbContext> options) : DbContext(options), IUnitOfWork
{
    public const string Schema = "authoring";

    public DbSet<LessonDraft> Drafts => Set<LessonDraft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthoringDbContext).Assembly);
    }
}

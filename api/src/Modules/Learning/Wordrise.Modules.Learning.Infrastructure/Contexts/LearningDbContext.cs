using Microsoft.EntityFrameworkCore;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Infrastructure.Contexts;

internal sealed class LearningDbContext(DbContextOptions<LearningDbContext> options) : DbContext(options), IUnitOfWork
{
    public const string Schema = "learning";

    public DbSet<Learner> Learners => Set<Learner>();

    public DbSet<WordProgress> WordProgress => Set<WordProgress>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LearningDbContext).Assembly);
    }
}

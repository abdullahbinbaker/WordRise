using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Wordrise.Shared.Infrastructure.Database;

namespace Wordrise.Modules.Learning.Infrastructure.Contexts;

/// <summary>Used by <c>dotnet ef</c> only, so migrations can be created without running the host.</summary>
internal sealed class LearningDbContextFactory : IDesignTimeDbContextFactory<LearningDbContext>
{
    public LearningDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LearningDbContext>();
        builder.UseWordriseDatabase(
            new DatabaseOptions { Provider = DatabaseProvider.Postgres, ConnectionString = "Host=localhost;Database=wordrise" },
            LearningDbContext.Schema);
        return new LearningDbContext(builder.Options);
    }
}

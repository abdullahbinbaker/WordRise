using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Wordrise.Shared.Infrastructure.Database;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Contexts;

/// <summary>Used by <c>dotnet ef</c> only, so migrations can be created without running the host.</summary>
internal sealed class AuthoringDbContextFactory : IDesignTimeDbContextFactory<AuthoringDbContext>
{
    public AuthoringDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AuthoringDbContext>();
        builder.UseWordriseDatabase(
            new DatabaseOptions { Provider = DatabaseProvider.Postgres, ConnectionString = "Host=localhost;Database=wordrise" },
            AuthoringDbContext.Schema);
        return new AuthoringDbContext(builder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Wordrise.Shared.Infrastructure.Database;

namespace Wordrise.Modules.Identity.Core.Data;

/// <summary>Used by <c>dotnet ef</c> only, so migrations can be created without running the host.</summary>
internal sealed class IdentityModuleDbContextFactory : IDesignTimeDbContextFactory<IdentityModuleDbContext>
{
    public IdentityModuleDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<IdentityModuleDbContext>();
        builder.UseWordriseDatabase(
            new DatabaseOptions { Provider = DatabaseProvider.Postgres, ConnectionString = "Host=localhost;Database=wordrise" },
            IdentityModuleDbContext.Schema);
        return new IdentityModuleDbContext(builder.Options);
    }
}

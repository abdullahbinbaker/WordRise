using Microsoft.EntityFrameworkCore;

namespace Wordrise.Shared.Infrastructure.Database;

public static class DatabaseExtensions
{
    /// <summary>Points a module's DbContext at the configured database, inside the module's own schema.</summary>
    public static DbContextOptionsBuilder UseWordriseDatabase(
        this DbContextOptionsBuilder builder,
        DatabaseOptions options,
        string schema) =>
        options.Provider switch
        {
            DatabaseProvider.Postgres => builder.UseNpgsql(
                options.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema)),
            DatabaseProvider.Sqlite => builder.UseSqlite(options.ConnectionString.Replace("{schema}", schema)),
            _ => throw new InvalidOperationException($"Unsupported database provider '{options.Provider}'."),
        };

    /// <summary>
    /// PostgreSQL is updated through migrations (when enabled); SQLite gets its tables created directly.
    /// </summary>
    public static Task PrepareAsync(this DbContext db, DatabaseOptions options, CancellationToken cancellationToken)
    {
        if (db.Database.IsNpgsql())
        {
            return options.ApplyMigrationsOnStartup ? db.Database.MigrateAsync(cancellationToken) : Task.CompletedTask;
        }

        return db.Database.EnsureCreatedAsync(cancellationToken);
    }
}

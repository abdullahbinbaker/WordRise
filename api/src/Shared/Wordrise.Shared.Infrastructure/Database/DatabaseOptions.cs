namespace Wordrise.Shared.Infrastructure.Database;

public enum DatabaseProvider
{
    Postgres,

    /// <summary>Local development and tests only: no server needed, no migrations.</summary>
    Sqlite,
}

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Postgres;

    /// <summary>
    /// For SQLite, <c>{schema}</c> is replaced with the module schema so each module gets its
    /// own file (EnsureCreated only creates tables in an empty database).
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    public bool ApplyMigrationsOnStartup { get; set; }
}

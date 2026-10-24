namespace Wordrise.Host;

/// <summary>
/// Docker Compose passes every variable of the server's .env file to the app, empty ones too
/// (<c>Media__Azure__Region=</c>). Left alone, an empty variable overrides the value in
/// <c>appsettings.json</c> with nothing. This makes an empty setting mean "not set", so the app
/// falls back to appsettings, which is what whoever left the line empty expects.
/// </summary>
public static class EmptyEnvironmentVariables
{
    /// <summary>
    /// Removes empty variables that look like settings (their name contains a double underscore,
    /// the way .NET writes <c>Section:Key</c> in the environment). Run before the app reads its configuration.
    /// </summary>
    /// <returns>The names that were removed.</returns>
    public static IReadOnlyList<string> Ignore()
    {
        List<string> removed = [];

        foreach (System.Collections.DictionaryEntry variable in Environment.GetEnvironmentVariables())
        {
            if (variable.Key is string name
                && name.Contains("__", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(variable.Value as string))
            {
                Environment.SetEnvironmentVariable(name, null);
                removed.Add(name);
            }
        }

        return removed;
    }
}

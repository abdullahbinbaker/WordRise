using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Wordrise.Testing;

/// <summary>Hosts the real API, every module on its own private in-memory SQLite database.</summary>
public sealed class WordriseApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "wordrise123";

    /// <summary>The schema of every module; add new modules here.</summary>
    private static readonly string[] ModuleSchemas = ["identity", "learning"];

    private readonly string _connectionString =
        $"Data Source=wordrise-tests-{Guid.NewGuid():N}-{{schema}};Mode=Memory;Cache=Shared";

    private readonly List<SqliteConnection> _keepAlive = [];

    public WordriseApiFactory()
    {
        // A shared in-memory database lives only while at least one connection is open.
        foreach (var schema in ModuleSchemas)
        {
            var connection = new SqliteConnection(_connectionString.Replace("{schema}", schema));
            connection.Open();
            _keepAlive.Add(connection);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionString", _connectionString);
        builder.UseSetting("Identity:AuthRequestsPerMinute", "1000");
    }

    /// <summary>HTTPS, because the auth cookie is Secure-only.</summary>
    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>Registers a fresh account and returns a client carrying its bearer token.</summary>
    public async Task<HttpClient> CreateSignedInClientAsync()
    {
        var client = CreateHttpsClient();
        var email = $"{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.ForEach(connection => connection.Dispose());
        }
    }

    private sealed record TokenResponse(string AccessToken);
}

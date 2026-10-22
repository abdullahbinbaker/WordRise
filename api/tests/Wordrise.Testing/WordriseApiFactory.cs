using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Wordrise.Testing;

/// <summary>Hosts the real API, every module on its own private in-memory SQLite database.</summary>
public sealed class WordriseApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "wordrise123";

    /// <summary>The schema of every module; add new modules here.</summary>
    private static readonly string[] ModuleSchemas = ["identity", "learning", "authoring"];

    private readonly string _connectionString =
        $"Data Source=wordrise-tests-{Guid.NewGuid():N}-{{schema}};Mode=Memory;Cache=Shared";

    private readonly List<SqliteConnection> _keepAlive = [];

    /// <summary>Extra settings for tests that need them; set before the first client is created.</summary>
    public Dictionary<string, string?> Settings { get; } = [];

    /// <summary>Replaces services (for example scripted fakes); set before the first client is created.</summary>
    public Action<IServiceCollection>? TestServices { get; set; }

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

        // Tests never call Claude, and the worker is driven by hand so runs are deterministic.
        builder.UseSetting("Authoring:Provider", "Sample");
        builder.UseSetting("Authoring:Worker:Enabled", "false");

        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        if (TestServices is not null)
        {
            builder.ConfigureTestServices(TestServices);
        }
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

    /// <summary>
    /// A client signed in as staff. The token is minted directly (there is no staff sign-up yet);
    /// <paramref name="withTwoFactor"/> false mimics a reviewer who signed in with a password only.
    /// </summary>
    public HttpClient CreateStaffClient(string role = "Reviewer", bool withTwoFactor = true)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "staff@example.com"),
            new(ClaimTypes.Role, role),
        ];
        if (withTwoFactor)
        {
            claims.Add(new Claim("amr", "mfa"));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.BearerScheme));
        var ticket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) },
            IdentityConstants.BearerScheme);

        var protector = Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>()
            .Get(IdentityConstants.BearerScheme).BearerTokenProtector;

        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", protector.Protect(ticket));
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

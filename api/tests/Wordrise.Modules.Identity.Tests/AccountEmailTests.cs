using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wordrise.Modules.Identity.Tests;

/// <summary>Confirming an email address, resetting a forgotten password, and what a locked-out account is told.</summary>
public sealed class AccountEmailTests : IClassFixture<WordriseApiFactory>
{
    private const string Password = WordriseApiFactory.Password;

    private sealed record Sent(string To, string Subject, string Body);

    /// <summary>Reads the emails the app writes to its log (the local-run mail transport) instead of sending them.</summary>
    private sealed class Outbox : ILoggerProvider, ILogger
    {
        public List<Sent> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> values && values.Any(v => v.Key == "Subject"))
            {
                string Value(string key) => values.First(v => v.Key == key).Value?.ToString() ?? "";
                lock (Messages)
                {
                    Messages.Add(new Sent(Value("To"), Value("Subject"), Value("Body")));
                }
            }
        }

        public void Dispose()
        {
        }
    }

    private readonly Outbox _outbox = new();
    private readonly WordriseApiFactory _factory;

    public AccountEmailTests(WordriseApiFactory factory)
    {
        // A separate host with confirmation switched on and the emails captured.
        _factory = factory;
    }

    private HttpClient Client()
    {
        var host = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Identity:RequireConfirmedEmail", "true");
            builder.UseSetting("Identity:WebBaseUrl", "https://site.example");
            builder.ConfigureServices(services => services.AddLogging(logging => logging.AddProvider(_outbox)));
        });
        return host.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    private static string LinkIn(Sent message) => Regex.Match(message.Body, @"https://\S+").Value;

    private static Dictionary<string, string> QueryOf(string link) =>
        System.Web.HttpUtility.ParseQueryString(new Uri(link).Query).AllKeys
            .ToDictionary(k => k!, k => System.Web.HttpUtility.ParseQueryString(new Uri(link).Query)[k]!);

    [Fact]
    public async Task A_new_account_cannot_sign_in_until_it_confirms_its_email()
    {
        var client = Client();
        var email = NewEmail();
        (await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password })).EnsureSuccessStatusCode();

        var before = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, before.StatusCode);
        Assert.Contains("NotAllowed", await before.Content.ReadAsStringAsync());

        var message = Assert.Single(_outbox.Messages, m => m.To == email);
        var link = LinkIn(message);
        Assert.StartsWith("https://site.example/confirm-email?", link);
        var query = QueryOf(link);
        var confirm = await client.GetAsync($"/api/v1/auth/confirmEmail?userId={query["userId"]}&code={Uri.EscapeDataString(query["code"])}");
        confirm.EnsureSuccessStatusCode();

        var after = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        after.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task The_confirmation_email_can_be_sent_again()
    {
        var client = Client();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });

        var resend = await client.PostAsJsonAsync("/api/v1/auth/resendConfirmationEmail", new { email });

        resend.EnsureSuccessStatusCode();
        Assert.Equal(2, _outbox.Messages.Count(m => m.To == email));
    }

    [Fact]
    public async Task A_forgotten_password_is_reset_from_the_emailed_link()
    {
        var client = Client();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });
        var confirmation = LinkIn(Assert.Single(_outbox.Messages, m => m.To == email));
        var q = QueryOf(confirmation);
        (await client.GetAsync($"/api/v1/auth/confirmEmail?userId={q["userId"]}&code={Uri.EscapeDataString(q["code"])}")).EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync("/api/v1/auth/forgotPassword", new { email })).EnsureSuccessStatusCode();
        var reset = _outbox.Messages.Last(m => m.To == email);
        var link = LinkIn(reset);
        Assert.StartsWith("https://site.example/reset-password?", link);
        var resetQuery = QueryOf(link);

        var done = await client.PostAsJsonAsync(
            "/api/v1/auth/resetPassword",
            new { email = resetQuery["email"], resetCode = resetQuery["code"], newPassword = "newpassword9" });
        done.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password })).StatusCode);
        (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "newpassword9" })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Asking_to_reset_an_unknown_address_looks_the_same_and_sends_nothing()
    {
        var client = Client();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/api/v1/auth/forgotPassword", new { email });

        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain(_outbox.Messages, m => m.To == email);
    }

    [Fact]
    public async Task Too_many_wrong_passwords_lock_the_account_and_the_answer_says_so()
    {
        var client = Client();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });
        var link = LinkIn(Assert.Single(_outbox.Messages, m => m.To == email));
        var q = QueryOf(link);
        (await client.GetAsync($"/api/v1/auth/confirmEmail?userId={q["userId"]}&code={Uri.EscapeDataString(q["code"])}")).EnsureSuccessStatusCode();

        HttpResponseMessage last = null!;
        for (var i = 0; i < 6; i++)
        {
            last = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "wrong-password1" });
        }

        Assert.Equal(HttpStatusCode.Unauthorized, last.StatusCode);
        Assert.Contains("LockedOut", await last.Content.ReadAsStringAsync());
    }
}

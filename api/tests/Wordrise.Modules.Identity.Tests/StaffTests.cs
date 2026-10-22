using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Wordrise.Modules.Identity.Tests;

/// <summary>Becoming staff: named an admin in settings, two-factor set up, then naming reviewers.</summary>
public sealed class StaffTests(WordriseApiFactory factory) : IClassFixture<WordriseApiFactory>
{
    private const string Password = WordriseApiFactory.Password;

    private sealed record Me(string Email, string[] Roles, bool TwoFactorEnabled, bool SignedInWithTwoFactor);

    private sealed record Token(string AccessToken);

    private sealed record TwoFactor(string SharedKey, bool IsTwoFactorEnabled, string[]? RecoveryCodes);

    private sealed record Member(Guid Id, string Email, string[] Roles, bool TwoFactorEnabled);

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        (await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password })).EnsureSuccessStatusCode();
        return email;
    }

    private static async Task<HttpClient> SignInAsync(WebApplicationFactoryHolder host, string email, string? code = null)
    {
        var client = host.Create();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password, twoFactorCode = code });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<Token>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    /// <summary>Restarts the app with this account named an admin, as an operator would.</summary>
    private WebApplicationFactoryHolder RestartWithAdmin(string email) => new(
        factory.WithWebHostBuilder(builder => builder.UseSetting("Identity:AdminEmails:0", email)));

    private static async Task<string> EnableTwoFactorAsync(HttpClient client)
    {
        var started = await client.PostAsJsonAsync("/api/v1/auth/manage/2fa", new { resetSharedKey = true });
        var key = (await started.Content.ReadFromJsonAsync<TwoFactor>())!.SharedKey;

        var enabled = await client.PostAsJsonAsync("/api/v1/auth/manage/2fa", new { enable = true, twoFactorCode = Totp(key) });
        enabled.EnsureSuccessStatusCode();
        Assert.True((await enabled.Content.ReadFromJsonAsync<TwoFactor>())!.IsTwoFactorEnabled);
        return key;
    }

    /// <summary>An authenticator app's six digits (RFC 6238: HMAC-SHA1, 30-second steps).</summary>
    private static string Totp(string base32Key)
    {
        var key = Base32Decode(base32Key);
        var step = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(step);
        }

        var hash = HMACSHA1.HashData(key, step);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string text)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in text.Replace(" ", string.Empty).TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)((buffer >> bits) & 0xFF));
            }
        }

        return [.. bytes];
    }

    [Fact]
    public async Task An_ordinary_account_is_not_staff_and_cannot_manage_staff()
    {
        var client = await factory.CreateSignedInClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/staff")).StatusCode);
    }

    [Fact]
    public async Task Naming_someone_an_admin_in_settings_is_not_enough_without_two_factor()
    {
        var email = await RegisterAsync(factory.CreateHttpsClient());
        var host = RestartWithAdmin(email);

        var client = await SignInAsync(host, email);
        var me = await client.GetFromJsonAsync<Me>("/api/v1/me");

        Assert.Contains("Admin", me!.Roles);
        Assert.False(me.SignedInWithTwoFactor);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/staff")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/lessons/drafts")).StatusCode);
    }

    [Fact]
    public async Task An_admin_with_two_factor_names_a_reviewer_who_then_reviews_lessons()
    {
        // 1. The owner registers, is named admin, and sets up the authenticator app.
        var adminEmail = await RegisterAsync(factory.CreateHttpsClient());
        var host = RestartWithAdmin(adminEmail);
        var key = await EnableTwoFactorAsync(await SignInAsync(host, adminEmail));

        // 2. Signing in again, now with the code, opens the staff doors.
        var admin = await SignInAsync(host, adminEmail, Totp(key));
        var me = await admin.GetFromJsonAsync<Me>("/api/v1/me");
        Assert.True(me!.TwoFactorEnabled);
        Assert.True(me.SignedInWithTwoFactor);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/staff")).StatusCode);

        // 3. A person with no account cannot be named; one who registered can.
        var unknown = await admin.PostAsJsonAsync("/api/v1/admin/staff/reviewers", new { email = "nobody@example.com" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var reviewerEmail = await RegisterAsync(factory.CreateHttpsClient());
        var grant = await admin.PostAsJsonAsync("/api/v1/admin/staff/reviewers", new { email = reviewerEmail });
        Assert.Equal(HttpStatusCode.NoContent, grant.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync("/api/v1/admin/staff/reviewers", new { email = reviewerEmail })).StatusCode);

        var staff = await admin.GetFromJsonAsync<Member[]>("/api/v1/admin/staff");
        var reviewer = staff!.Single(m => m.Email == reviewerEmail);
        Assert.Equal(["Reviewer"], reviewer.Roles);

        // 4. The reviewer sets up two-factor too, and reaches the review queue; a reviewer cannot manage staff.
        var reviewerKey = await EnableTwoFactorAsync(await SignInAsync(host, reviewerEmail));
        var signedIn = await SignInAsync(host, reviewerEmail, Totp(reviewerKey));
        Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync("/api/v1/admin/lessons/drafts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await signedIn.GetAsync("/api/v1/admin/staff")).StatusCode);

        // 5. Taking the role away works for the next sign-in.
        var revoke = await admin.DeleteAsync($"/api/v1/admin/staff/reviewers/{reviewer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        var after = await SignInAsync(host, reviewerEmail, Totp(reviewerKey));
        Assert.Equal(HttpStatusCode.Forbidden, (await after.GetAsync("/api/v1/admin/lessons/drafts")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/v1/admin/staff/reviewers/{reviewer.Id}")).StatusCode);
    }

    /// <summary>Creates https clients for a restarted copy of the app.</summary>
    private sealed class WebApplicationFactoryHolder(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> inner)
    {
        public HttpClient Create() => inner.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }
}

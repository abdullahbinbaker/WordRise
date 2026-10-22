using System.Net;
using System.Net.Http.Json;

namespace Wordrise.Modules.Identity.Tests;

public sealed class AuthAndProfileTests(WordriseApiFactory factory) : IClassFixture<WordriseApiFactory>
{
    private const string Password = WordriseApiFactory.Password;

    private sealed record MeResponse(Guid Id, string Email, string? DisplayName, string[] Roles);

    private sealed record ChildResponse(Guid Id, string DisplayName, string AvatarKey, int BirthYear, int DailyMinutesLimit);

    private static object ChildRequest(string name = "خالد") => new
    {
        displayName = name,
        avatarKey = "lion",
        birthYear = DateTime.UtcNow.Year - 6,
    };

    private Task<HttpClient> SignedInClientAsync() => factory.CreateSignedInClientAsync();

    [Fact]
    public async Task Me_requires_sign_in()
    {
        var response = await factory.CreateHttpsClient().GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bearer_sign_in_returns_the_account()
    {
        var client = await SignedInClientAsync();

        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");

        Assert.NotNull(me);
        Assert.Empty(me.Roles);
    }

    [Fact]
    public async Task Cookie_sign_in_works_for_the_website()
    {
        var client = factory.CreateHttpsClient();
        var email = $"{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password });

        var login = await client.PostAsJsonAsync("/api/v1/auth/login?useCookies=true", new { email, password = Password });
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(email, me!.Email);
    }

    [Fact]
    public async Task Display_name_can_be_set()
    {
        var client = await SignedInClientAsync();

        var update = await client.PutAsJsonAsync("/api/v1/me", new { displayName = "  أبو خالد  " });
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        Assert.Equal("أبو خالد", me!.DisplayName);
    }

    [Fact]
    public async Task First_child_makes_the_account_a_parent_and_last_one_removes_it()
    {
        var client = await SignedInClientAsync();

        var created = await client.PostAsJsonAsync("/api/v1/me/children", ChildRequest());
        var child = await created.Content.ReadFromJsonAsync<ChildResponse>();
        var asParent = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(20, child!.DailyMinutesLimit);
        Assert.Contains("Parent", asParent!.Roles);

        var deleted = await client.DeleteAsync($"/api/v1/me/children/{child.Id}");
        var afterDelete = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.DoesNotContain("Parent", afterDelete!.Roles);
    }

    [Fact]
    public async Task Invalid_child_profile_is_rejected_with_field_errors()
    {
        var client = await SignedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/me/children",
            new { displayName = "", avatarKey = "dragon", birthYear = DateTime.UtcNow.Year - 30 });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("displayName", body);
        Assert.Contains("avatarKey", body);
        Assert.Contains("birthYear", body);
    }

    [Fact]
    public async Task A_parent_cannot_see_or_change_another_parents_child()
    {
        var owner = await SignedInClientAsync();
        var stranger = await SignedInClientAsync();
        var created = await owner.PostAsJsonAsync("/api/v1/me/children", ChildRequest());
        var child = await created.Content.ReadFromJsonAsync<ChildResponse>();

        var strangerList = await stranger.GetFromJsonAsync<ChildResponse[]>("/api/v1/me/children");
        var strangerUpdate = await stranger.PutAsJsonAsync($"/api/v1/me/children/{child!.Id}", ChildRequest("changed"));
        var strangerDelete = await stranger.DeleteAsync($"/api/v1/me/children/{child.Id}");

        Assert.Empty(strangerList!);
        Assert.Equal(HttpStatusCode.NotFound, strangerUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, strangerDelete.StatusCode);
    }
}

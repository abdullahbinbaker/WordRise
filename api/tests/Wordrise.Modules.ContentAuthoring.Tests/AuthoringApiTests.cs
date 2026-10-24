using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;

namespace Wordrise.Modules.ContentAuthoring.Tests;

/// <summary>The admin endpoints end to end, with the built-in sample writer and reviewer.</summary>
public sealed class AuthoringApiTests(WordriseApiFactory factory) : IClassFixture<WordriseApiFactory>
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private const string Drafts = "/api/v1/admin/lessons/drafts";

    internal sealed record Created(Guid Id);

    internal sealed record Detail(Guid Id, string Status, string? Error, int CyclesUsed, JsonElement? Content, JsonElement[] Revisions, Guid? PublishedLessonId, int? PublishedLessonNumber);

    private sealed record Published(Guid LessonId, int Number);

    private sealed record LessonSummary(Guid Id, string Title);

    internal static async Task<Guid> CreateDraftAsync(HttpClient staff, string scenario = "grocery store", int step = 4, int wordCount = 4)
    {
        var response = await staff.PostAsJsonAsync(Drafts, new { scenario, grammarStep = step, wordCount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Created>(Json))!.Id;
    }

    internal static Task<Detail?> GetAsync(HttpClient staff, Guid id) => staff.GetFromJsonAsync<Detail>($"{Drafts}/{id}", Json);

    private Task RunWorkerAsync() => factory.Services.GetRequiredService<IDraftQueueRunner>().RunOnceAsync(CancellationToken.None);

    [Fact]
    public async Task Anonymous_visitors_are_turned_away()
    {
        var response = await factory.CreateHttpsClient().GetAsync(Drafts);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Learners_cannot_reach_the_review_queue()
    {
        var learner = await factory.CreateSignedInClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await learner.GetAsync(Drafts)).StatusCode);
    }

    [Fact]
    public async Task Staff_without_two_factor_are_turned_away()
    {
        var reviewer = factory.CreateStaffClient(withTwoFactor: false);

        Assert.Equal(HttpStatusCode.Forbidden, (await reviewer.GetAsync(Drafts)).StatusCode);
    }

    [Theory]
    [InlineData("Reviewer")]
    [InlineData("Admin")]
    public async Task Staff_with_two_factor_get_in(string role)
    {
        var staff = factory.CreateStaffClient(role);

        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync(Drafts)).StatusCode);
        var curriculum = await staff.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/lessons/curriculum", Json);
        Assert.Equal(34, curriculum!.Length);
    }

    [Fact]
    public async Task A_new_request_waits_in_the_queue()
    {
        var staff = factory.CreateStaffClient();

        var id = await CreateDraftAsync(staff);

        var draft = await GetAsync(staff, id);
        Assert.Equal("queued", draft!.Status);
        Assert.Null(draft.Content);
    }

    [Theory]
    [InlineData("", 4)]
    [InlineData("x", 4)]
    [InlineData("grocery store", 0)]
    [InlineData("grocery store", 99)]
    public async Task A_bad_request_is_refused(string scenario, int step)
    {
        var response = await factory.CreateStaffClient().PostAsJsonAsync(Drafts, new { scenario, grammarStep = step });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_drafts_are_not_found()
    {
        var response = await factory.CreateStaffClient().GetAsync($"{Drafts}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_list_can_be_filtered_by_status()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);

        var queued = await staff.GetFromJsonAsync<JsonElement[]>($"{Drafts}?status=queued", Json);
        var approved = await staff.GetFromJsonAsync<JsonElement[]>($"{Drafts}?status=published", Json);

        Assert.Contains(queued!, item => item.GetProperty("id").GetGuid() == id);
        Assert.DoesNotContain(approved!, item => item.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task A_lesson_cannot_be_approved_before_the_ai_has_finished_with_it()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);

        var response = await staff.PostAsync($"{Drafts}/{id}/approve", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_worker_writes_the_lesson_and_a_person_publishes_it_to_learners()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);

        await RunWorkerAsync();

        var written = await GetAsync(staff, id);
        Assert.Equal("aiApproved", written!.Status);
        Assert.Single(written.Revisions);
        Assert.NotNull(written.Content);

        var approve = await staff.PostAsync($"{Drafts}/{id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var published = await approve.Content.ReadFromJsonAsync<Published>(Json);

        // The learners' side now has the lesson, under the draft's id.
        var learner = await factory.CreateSignedInClientAsync();
        var lesson = await learner.GetFromJsonAsync<LessonSummary>($"/api/v1/learning/lessons/{id}", Json);
        Assert.Equal(id, lesson!.Id);
        Assert.Equal(published!.LessonId, id);
        Assert.Equal("published", (await GetAsync(staff, id))!.Status);
    }

    [Fact]
    public async Task Approving_twice_publishes_once()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);
        await RunWorkerAsync();

        var first = await (await staff.PostAsync($"{Drafts}/{id}/approve", null)).Content.ReadFromJsonAsync<Published>(Json);
        var second = await (await staff.PostAsync($"{Drafts}/{id}/approve", null)).Content.ReadFromJsonAsync<Published>(Json);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Sending_a_lesson_back_needs_a_note_and_requeues_it()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);
        await RunWorkerAsync();

        var withoutNote = await staff.PostAsJsonAsync($"{Drafts}/{id}/reject", new { note = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, withoutNote.StatusCode);

        var rejected = await staff.PostAsJsonAsync($"{Drafts}/{id}/reject", new { note = "Make the dialogue shorter." });
        Assert.Equal(HttpStatusCode.NoContent, rejected.StatusCode);
        Assert.Equal("queued", (await GetAsync(staff, id))!.Status);
    }

    [Fact]
    public async Task Only_a_failed_draft_can_be_requeued()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);

        var response = await staff.PostAsync($"{Drafts}/{id}/requeue", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("fr", "ar")]
    [InlineData("en", "fr")]
    [InlineData("xx", "yy")]
    public async Task A_course_that_does_not_exist_yet_is_refused(string target, string native)
    {
        var response = await factory.CreateStaffClient().PostAsJsonAsync(
            Drafts,
            new { scenario = "grocery store", grammarStep = 4, targetLanguage = target, nativeLanguage = native });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_lesson_carries_its_languages_from_the_brief_to_the_learners()
    {
        var staff = factory.CreateStaffClient();
        var id = await CreateDraftAsync(staff);

        // Other tests share this app and leave drafts waiting; the worker takes a few per run.
        for (var run = 0; run < 20 && (await GetAsync(staff, id))!.Status != "aiApproved"; run++)
        {
            await RunWorkerAsync();
        }

        (await staff.PostAsync($"{Drafts}/{id}/approve", null)).EnsureSuccessStatusCode();

        var draft = await staff.GetFromJsonAsync<JsonElement>($"{Drafts}/{id}", Json);
        var learner = await factory.CreateSignedInClientAsync();
        var lesson = await learner.GetFromJsonAsync<JsonElement>($"/api/v1/learning/lessons/{id}", Json);

        Assert.Equal("en", draft.GetProperty("brief").GetProperty("targetLanguage").GetString());
        Assert.Equal("ar", draft.GetProperty("brief").GetProperty("nativeLanguage").GetString());
        Assert.Equal("en", lesson.GetProperty("targetLanguage").GetString());
        Assert.Equal("ar", lesson.GetProperty("nativeLanguage").GetString());
        Assert.False(string.IsNullOrWhiteSpace(lesson.GetProperty("words")[0].GetProperty("target").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(lesson.GetProperty("words")[0].GetProperty("native").GetString()));
    }
}

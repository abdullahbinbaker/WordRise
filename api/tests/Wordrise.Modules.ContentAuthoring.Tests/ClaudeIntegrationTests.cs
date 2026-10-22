using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.TestSupport;

namespace Wordrise.Modules.ContentAuthoring.Tests;

/// <summary>
/// The real Claude client against a stand-in Anthropic server: checks what we send and how we
/// react to what comes back, without a key or any cost.
/// </summary>
public sealed class ClaudeIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly List<JsonElement> _requests = [];
    private readonly List<string?> _keys = [];
    private readonly Queue<Func<JsonElement, (string Text, string StopReason)>> _answers = new();
    private WebApplication _server = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _server = builder.Build();
        _server.MapPost("/v1/messages", async (HttpContext http) =>
        {
            using var body = await JsonDocument.ParseAsync(http.Request.Body);
            var request = body.RootElement.Clone();
            _requests.Add(request);
            _keys.Add(string.Join(";", http.Request.Headers.Select(h => h.Key + "=" + h.Value)));

            var (text, stopReason) = _answers.Dequeue()(request);
            return Results.Json(new
            {
                id = "msg_test",
                type = "message",
                role = "assistant",
                model = request.GetProperty("model").GetString(),
                content = new[] { new { type = "text", text } },
                stop_reason = stopReason,
                stop_sequence = (string?)null,
                usage = new { input_tokens = 100, output_tokens = 200 },
            });
        });
        await _server.StartAsync();
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private string Url => _server.Urls.First();

    private WordriseApiFactory ClaudeFactory(string? apiKey = "test-key")
    {
        var factory = new WordriseApiFactory();
        factory.Settings["Authoring:Provider"] = "Claude";
        factory.Settings["Authoring:Claude:ApiKey"] = apiKey;
        factory.Settings["Authoring:Claude:BaseUrl"] = Url;
        return factory;
    }

    private static string Lesson(LessonBrief brief) => JsonSerializer.Serialize(ContentFixtures.ValidContent(brief), Web);

    private static string Review(ReviewReport report) => JsonSerializer.Serialize(
        new
        {
            summary = report.Summary,
            criteria = report.Criteria.Select(c => new { criterion = c.Criterion.ToString(), passed = c.Passed, comment = c.Comment }),
            issues = report.Issues.Select(i => new { severity = i.Severity.ToString().ToLowerInvariant(), where = i.Where, problem = i.Problem, suggestion = i.Suggestion }),
        },
        Web);

    private static async Task<AuthoringApiTests.Detail> RunDraftAsync(WordriseApiFactory factory)
    {
        using var staff = factory.CreateStaffClient();
        var id = await AuthoringApiTests.CreateDraftAsync(staff);
        await factory.Services.GetRequiredService<IDraftQueueRunner>().RunOnceAsync(CancellationToken.None);
        return (await AuthoringApiTests.GetAsync(staff, id))!;
    }

    [Fact]
    public async Task The_writer_and_reviewer_send_the_agreed_requests()
    {
        var brief = ContentFixtures.Brief(step: 4, words: 4);
        _answers.Enqueue(_ => (Lesson(brief), "end_turn"));
        _answers.Enqueue(_ => (Review(ContentFixtures.Passing()), "end_turn"));
        using var factory = ClaudeFactory();

        var draft = await RunDraftAsync(factory);

        Assert.Equal("aiApproved", draft.Status);
        Assert.Equal(2, _requests.Count);
        Assert.All(_keys, key => Assert.Contains("test-key", key));

        var write = _requests[0];
        Assert.Equal("claude-opus-5-5", write.GetProperty("model").GetString());
        Assert.Equal(16000, write.GetProperty("max_tokens").GetInt32());
        Assert.Equal("high", write.GetProperty("output_config").GetProperty("effort").GetString());
        Assert.Equal("json_schema", write.GetProperty("output_config").GetProperty("format").GetProperty("type").GetString());
        Assert.False(write.TryGetProperty("temperature", out _));
        Assert.False(write.TryGetProperty("thinking", out _));
        Assert.Contains("grocery store", write.GetProperty("messages")[0].GetProperty("content").ToString());
        Assert.False(string.IsNullOrWhiteSpace(write.GetProperty("system")[0].GetProperty("text").GetString()));

        var review = _requests[1];
        Assert.Equal("claude-sonnet-5-5", review.GetProperty("model").GetString());
        Assert.Equal(8000, review.GetProperty("max_tokens").GetInt32());
        Assert.Equal("medium", review.GetProperty("output_config").GetProperty("effort").GetString());
    }

    [Fact]
    public async Task A_refusal_fails_the_draft_with_a_clear_reason()
    {
        _answers.Enqueue(_ => ("", "refusal"));
        using var factory = ClaudeFactory();

        var draft = await RunDraftAsync(factory);

        Assert.Equal("failed", draft.Status);
        Assert.Contains("declined", draft.Error);
    }

    [Fact]
    public async Task An_answer_cut_short_fails_the_draft_and_names_the_setting_to_raise()
    {
        _answers.Enqueue(_ => ("{\"title\":\"", "max_tokens"));
        using var factory = ClaudeFactory();

        var draft = await RunDraftAsync(factory);

        Assert.Equal("failed", draft.Status);
        Assert.Contains("MaxTokens", draft.Error);
    }

    [Fact]
    public async Task An_answer_that_is_not_json_fails_the_draft()
    {
        _answers.Enqueue(_ => ("Sure, here is your lesson!", "end_turn"));
        using var factory = ClaudeFactory();

        var draft = await RunDraftAsync(factory);

        Assert.Equal("failed", draft.Status);
        Assert.Contains("not the expected JSON", draft.Error);
    }

    [Fact]
    public async Task The_reviewers_complaints_are_sent_to_the_writer()
    {
        var brief = ContentFixtures.Brief(step: 4, words: 4);
        _answers.Enqueue(_ => (Lesson(brief), "end_turn"));
        _answers.Enqueue(_ => (Review(ContentFixtures.Failing("Unnatural greeting.")), "end_turn"));
        _answers.Enqueue(_ => (Lesson(brief), "end_turn"));
        _answers.Enqueue(_ => (Review(ContentFixtures.Passing()), "end_turn"));
        using var factory = ClaudeFactory();

        var draft = await RunDraftAsync(factory);

        Assert.Equal("aiApproved", draft.Status);
        Assert.Equal(4, _requests.Count);
        Assert.Contains("Unnatural greeting.", _requests[2].GetProperty("messages")[0].GetProperty("content").ToString());
    }

    [Fact]
    public async Task Without_a_key_the_draft_fails_and_says_where_to_put_it()
    {
        using var factory = ClaudeFactory(apiKey: "");

        var draft = await RunDraftAsync(factory);

        Assert.Equal("failed", draft.Status);
        Assert.Contains("Authoring__Claude__ApiKey", draft.Error);
        Assert.Empty(_requests);
    }
}

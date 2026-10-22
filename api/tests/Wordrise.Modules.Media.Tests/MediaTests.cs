using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Media.Tests;

/// <summary>
/// Recording and serving audio, against a stand-in for Azure Speech: checks what is asked of it
/// and how every outcome is handled, without an Azure account.
/// </summary>
public sealed class MediaTests : IAsyncLifetime
{
    private static readonly byte[] FakeMp3 = [0x49, 0x44, 0x33, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    private readonly List<(string Ssml, IDictionary<string, string> Headers)> _requests = [];
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"wordrise-media-{Guid.NewGuid():N}");
    private WebApplication _azure = null!;
    private int _status = 200;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _azure = builder.Build();
        _azure.MapPost("/cognitiveservices/v1", async (HttpContext http) =>
        {
            using var reader = new StreamReader(http.Request.Body, Encoding.UTF8);
            var headers = http.Request.Headers.ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value.ToString());
            lock (_requests)
            {
                _requests.Add((reader.ReadToEndAsync().GetAwaiter().GetResult(), headers));
            }

            return _status == 200
                ? Results.File(FakeMp3, "audio/mpeg")
                : Results.StatusCode(_status);
        });
        await _azure.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _azure.DisposeAsync();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private WordriseApiFactory Factory(bool azure = true)
    {
        var factory = new WordriseApiFactory();
        factory.Settings["Media:Storage:Path"] = _folder;
        if (azure)
        {
            factory.Settings["Media:Provider"] = "Azure";
            factory.Settings["Media:Azure:Key"] = "azure-test-key";
            factory.Settings["Media:Azure:Region"] = "westeurope";
            factory.Settings["Media:Azure:Endpoint"] = _azure.Urls.First() + "/cognitiveservices/v1";
        }

        return factory;
    }

    private static async Task<AudioGenerationResult> EnsureAsync(WordriseApiFactory factory, params SpokenText[] texts)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IAudioLibrary>().EnsureAsync(texts, CancellationToken.None);
    }

    private static string Url(string text, bool slow = false) => $"/api/v1/media/audio/{AudioId.For(text, slow)}";

    [Fact]
    public void A_recording_is_named_from_its_text_alone()
    {
        Assert.Equal(AudioId.For("apple"), AudioId.For("  apple "));
        Assert.Equal(AudioId.For("I want  an apple."), AudioId.For("I want an apple."));
        Assert.NotEqual(AudioId.For("apple"), AudioId.For("apple", slow: true));
        Assert.NotEqual(AudioId.For("apple"), AudioId.For("Apple"));
        Assert.Matches("^[0-9a-f]{32}$", AudioId.For("apple"));

        // The website computes the same names: this value is repeated in web/src/app/core/audio-id.spec.ts.
        Assert.Equal("7441e112be2685a40ab86b37fe161ea1", AudioId.For("apple"));
    }

    [Fact]
    public async Task Missing_recordings_are_made_with_the_right_voice_and_format()
    {
        using var factory = Factory();

        var result = await EnsureAsync(
            factory,
            new SpokenText("apple"),
            new SpokenText("Hello! Can I help you?", SpeakerVoice.Secondary),
            new SpokenText("I can buy apple.", Slow: true));

        Assert.Equal(new AudioGenerationResult(true, 3, 3, 0, 0), result);
        Assert.Equal(3, _requests.Count);
        Assert.All(_requests, r => Assert.Equal("azure-test-key", r.Headers["ocp-apim-subscription-key"]));
        Assert.All(_requests, r => Assert.Equal("audio-24khz-48kbitrate-mono-mp3", r.Headers["x-microsoft-outputformat"]));
        Assert.All(_requests, r => Assert.StartsWith("application/ssml+xml", r.Headers["content-type"]));

        Assert.Contains(_requests, r => r.Ssml.Contains("en-US-JennyNeural") && r.Ssml.Contains(">apple</voice>"));
        Assert.Contains(_requests, r => r.Ssml.Contains("en-US-GuyNeural") && r.Ssml.Contains("Hello! Can I help you?"));
        Assert.Contains(_requests, r => r.Ssml.Contains("<prosody rate=\"-40%\">I can buy apple.</prosody>"));
    }

    [Fact]
    public async Task Text_is_escaped_so_it_cannot_break_the_request()
    {
        using var factory = Factory();

        await EnsureAsync(factory, new SpokenText("Tom & Jerry <3"));

        Assert.Contains("Tom &amp; Jerry &lt;3", _requests.Single().Ssml);
    }

    [Fact]
    public async Task Existing_recordings_and_repeated_texts_are_not_made_again()
    {
        using var factory = Factory();

        await EnsureAsync(factory, new SpokenText("apple"), new SpokenText(" apple"), new SpokenText("milk"));
        var again = await EnsureAsync(factory, new SpokenText("apple"), new SpokenText("milk"));

        Assert.Equal(2, _requests.Count);
        Assert.Equal(new AudioGenerationResult(true, 2, 0, 2, 0), again);
    }

    [Fact]
    public async Task A_failing_recording_is_counted_and_does_not_throw()
    {
        _status = 401;
        using var factory = Factory();

        var result = await EnsureAsync(factory, new SpokenText("apple"), new SpokenText("milk"));

        Assert.Equal(new AudioGenerationResult(true, 2, 0, 0, 2), result);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateHttpsClient().GetAsync(Url("apple"))).StatusCode);
    }

    [Fact]
    public async Task Without_a_provider_nothing_is_attempted()
    {
        using var factory = Factory(azure: false);

        var result = await EnsureAsync(factory, new SpokenText("apple"));

        Assert.Equal(new AudioGenerationResult(false, 1, 0, 0, 0), result);
        Assert.Empty(_requests);
    }

    [Fact]
    public async Task A_provider_without_a_key_is_treated_as_not_set_up()
    {
        using var factory = Factory();
        factory.Settings["Media:Azure:Key"] = "";

        var result = await EnsureAsync(factory, new SpokenText("apple"));

        Assert.False(result.Enabled);
        Assert.Empty(_requests);
    }

    [Fact]
    public async Task A_recording_is_served_to_anyone_and_cached_for_a_day_then_checked()
    {
        using var factory = Factory();
        await EnsureAsync(factory, new SpokenText("apple"));

        var response = await factory.CreateHttpsClient().GetAsync(Url("apple"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("audio/mpeg", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("immutable", response.Headers.CacheControl?.ToString());
        Assert.Equal(TimeSpan.FromDays(1), response.Headers.CacheControl?.MaxAge);
        Assert.NotNull(response.Headers.ETag);

        var again = new HttpRequestMessage(HttpMethod.Get, Url("apple")) { Headers = { IfNoneMatch = { response.Headers.ETag } } };
        Assert.Equal(HttpStatusCode.NotModified, (await factory.CreateHttpsClient().SendAsync(again)).StatusCode);
        Assert.Equal(FakeMp3, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Parts_of_a_recording_can_be_requested_as_Safari_does()
    {
        using var factory = Factory();
        await EnsureAsync(factory, new SpokenText("apple"));
        var request = new HttpRequestMessage(HttpMethod.Get, Url("apple")) { Headers = { Range = new RangeHeaderValue(0, 3) } };

        var response = await factory.CreateHttpsClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(FakeMp3[..4], await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef")]
    [InlineData("..%2f..%2fappsettings")]
    [InlineData("not-an-id")]
    [InlineData("7441E112BE2685A40AB86B37FE161EA1")]
    public async Task Unknown_or_malformed_names_are_not_found(string id)
    {
        using var factory = Factory();

        var response = await factory.CreateHttpsClient().GetAsync($"/api/v1/media/audio/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publishing_a_lesson_records_everything_the_learner_can_listen_to()
    {
        using var factory = Factory();
        using var staff = factory.CreateStaffClient();

        var id = await PublishNewLessonAsync(factory, staff);

        var lesson = await factory.CreateHttpsClient().GetAsync(Url("bread"));
        Assert.Equal(HttpStatusCode.OK, lesson.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateHttpsClient().GetAsync(Url("Hello! Can I help you?"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateHttpsClient().GetAsync(Url("I can buy bread.", slow: true))).StatusCode);
        Assert.NotEqual(Guid.Empty, id);
        Assert.Contains(_requests, r => r.Ssml.Contains("en-US-GuyNeural") && r.Ssml.Contains("Hello! Can I help you?"));
    }

    [Fact]
    public async Task A_speech_outage_does_not_stop_a_lesson_from_being_published()
    {
        _status = 503;
        using var factory = Factory();
        using var staff = factory.CreateStaffClient();

        var id = await PublishNewLessonAsync(factory, staff);

        var learner = await factory.CreateSignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await learner.GetAsync($"/api/v1/learning/lessons/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateHttpsClient().GetAsync(Url("bread"))).StatusCode);
    }

    [Fact]
    public async Task Staff_can_record_what_is_missing_once_the_provider_works()
    {
        _status = 503;
        using var factory = Factory();
        using var staff = factory.CreateStaffClient();
        await PublishNewLessonAsync(factory, staff);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateHttpsClient().GetAsync(Url("bread"))).StatusCode);

        _status = 200;
        var response = await staff.PostAsync("/api/v1/admin/lessons/audio", null);
        var result = await response.Content.ReadFromJsonAsync<AudioGenerationResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result!.Created > 0);
        Assert.Equal(0, result.Failed);
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateHttpsClient().GetAsync(Url("bread"))).StatusCode);
    }

    [Fact]
    public async Task Only_staff_can_start_a_recording_run()
    {
        using var factory = Factory();

        var anonymous = await factory.CreateHttpsClient().PostAsync("/api/v1/admin/lessons/audio", null);
        var learner = await (await factory.CreateSignedInClientAsync()).PostAsync("/api/v1/admin/lessons/audio", null);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, learner.StatusCode);
    }

    /// <summary>Asks for a sample lesson, lets the worker write it, and approves it.</summary>
    private static async Task<Guid> PublishNewLessonAsync(WordriseApiFactory factory, HttpClient staff)
    {
        var created = await staff.PostAsJsonAsync(
            "/api/v1/admin/lessons/drafts",
            new { scenario = "grocery store", grammarStep = 4, wordCount = 5 });
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<Created>())!.Id;

        await factory.Services.GetRequiredService<IDraftQueueRunner>().RunOnceAsync(CancellationToken.None);
        (await staff.PostAsync($"/api/v1/admin/lessons/drafts/{id}/approve", null)).EnsureSuccessStatusCode();
        return id;
    }

    private sealed record Created(Guid Id);
}

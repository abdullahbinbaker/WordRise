using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Modules.ContentAuthoring.TestSupport;

namespace Wordrise.Modules.ContentAuthoring.Tests;

/// <summary>The write-and-review loop through the real app, with a writer and reviewer that follow a script.</summary>
public sealed class AuthoringPipelineTests : IDisposable
{
    private readonly Script _script = new();
    private readonly WordriseApiFactory _factory;
    private readonly HttpClient _staff;

    public AuthoringPipelineTests()
    {
        _factory = new WordriseApiFactory
        {
            TestServices = services =>
            {
                services.AddScoped<ILessonWriter>(_ => new ScriptedWriter(_script));
                services.AddScoped<ILessonReviewer>(_ => new ScriptedReviewer(_script));
            },
        };
        _staff = _factory.CreateStaffClient();
    }

    public void Dispose()
    {
        _staff.Dispose();
        _factory.Dispose();
    }

    private Task<int> RunAsync() => _factory.Services.GetRequiredService<IDraftQueueRunner>().RunOnceAsync(CancellationToken.None);

    private Task<Guid> NewDraftAsync() => AuthoringApiTests.CreateDraftAsync(_staff);

    private async Task<string> StatusAsync(Guid id) => (await AuthoringApiTests.GetAsync(_staff, id))!.Status;

    [Fact]
    public async Task A_lesson_the_reviewer_likes_goes_to_a_person_after_one_attempt()
    {
        _script.Reviews.Enqueue(ContentFixtures.Passing());
        var id = await NewDraftAsync();

        await RunAsync();

        Assert.Equal("aiApproved", await StatusAsync(id));
        Assert.Equal(1, _script.WriteCalls);
        Assert.Equal(0, _script.ReviseCalls);
    }

    [Fact]
    public async Task The_writer_fixes_what_the_reviewer_finds()
    {
        _script.Reviews.Enqueue(ContentFixtures.Failing());
        _script.Reviews.Enqueue(ContentFixtures.Passing());
        var id = await NewDraftAsync();

        await RunAsync();

        var after = (await AuthoringApiTests.GetAsync(_staff, id))!;
        Assert.Equal("aiApproved", after.Status);
        Assert.Equal(2, after.Revisions.Length);
        Assert.Equal(1, _script.ReviseCalls);
        Assert.Contains("Say it the way a cashier would.", _script.LastReviewSeenByWriter);
    }

    [Fact]
    public async Task A_lesson_that_never_passes_is_flagged_for_a_person_after_the_allowed_attempts()
    {
        for (var i = 0; i < 5; i++)
        {
            _script.Reviews.Enqueue(ContentFixtures.Failing());
        }

        var id = await NewDraftAsync();

        await RunAsync();

        var after = (await AuthoringApiTests.GetAsync(_staff, id))!;
        Assert.Equal("needsAttention", after.Status);
        Assert.Equal(3, after.Revisions.Length);
    }

    [Fact]
    public async Task A_broken_structure_is_caught_without_paying_for_the_reviewer()
    {
        _script.Content = _ => new DraftContent { Title = "فارغ" };
        var id = await NewDraftAsync();

        await RunAsync();

        Assert.Equal("needsAttention", await StatusAsync(id));
        Assert.Equal(0, _script.ReviewCalls);
    }

    [Fact]
    public async Task A_writer_that_breaks_fails_the_draft_and_it_can_be_requeued()
    {
        _script.WriterError = new InvalidOperationException("The service is down.");
        var id = await NewDraftAsync();

        await RunAsync();

        var failed = (await AuthoringApiTests.GetAsync(_staff, id))!;
        Assert.Equal("failed", failed.Status);
        Assert.Contains("The service is down.", failed.Error);

        _script.WriterError = null;
        _script.Reviews.Enqueue(ContentFixtures.Passing());
        (await _staff.PostAsync($"/api/v1/admin/lessons/drafts/{id}/requeue", null)).EnsureSuccessStatusCode();
        await RunAsync();

        Assert.Equal("aiApproved", await StatusAsync(id));
    }

    [Fact]
    public async Task A_note_from_a_person_reaches_the_writer_on_the_next_run()
    {
        _script.Reviews.Enqueue(ContentFixtures.Passing());
        var id = await NewDraftAsync();
        await RunAsync();

        var reject = await _staff.PostAsJsonAsync($"/api/v1/admin/lessons/drafts/{id}/reject", new { note = "Use shorter sentences." });
        reject.EnsureSuccessStatusCode();
        _script.Reviews.Enqueue(ContentFixtures.Passing());
        await RunAsync();

        Assert.Contains("Use shorter sentences.", _script.LastHumanFeedback);
    }

    [Fact]
    public async Task Words_already_taught_are_passed_to_the_next_lesson()
    {
        _script.Reviews.Enqueue(ContentFixtures.Passing());
        var first = await NewDraftAsync();
        await RunAsync();
        (await _staff.PostAsync($"/api/v1/admin/lessons/drafts/{first}/approve", null)).EnsureSuccessStatusCode();

        _script.Reviews.Enqueue(ContentFixtures.Passing());
        await NewDraftAsync();
        await RunAsync();

        Assert.Contains("apple", _script.LastKnownWords);
    }

    /// <summary>What the fake writer and reviewer do next, and what they were told.</summary>
    private sealed class Script
    {
        public Queue<ReviewReport> Reviews { get; } = new();

        public Func<LessonBrief, DraftContent> Content { get; set; } = ContentFixtures.ValidContent;

        public Exception? WriterError { get; set; }

        public int WriteCalls { get; set; }

        public int ReviseCalls { get; set; }

        public int ReviewCalls { get; set; }

        public string LastReviewSeenByWriter { get; set; } = string.Empty;

        public IReadOnlyList<string> LastHumanFeedback { get; set; } = [];

        public IReadOnlyList<string> LastKnownWords { get; set; } = [];
    }

    private sealed class ScriptedWriter(Script script) : ILessonWriter
    {
        public Task<DraftContent> WriteAsync(AuthoringContext context, CancellationToken cancellationToken)
        {
            script.WriteCalls++;
            script.LastHumanFeedback = context.HumanFeedback;
            script.LastKnownWords = context.KnownWords;
            return script.WriterError is { } error
                ? Task.FromException<DraftContent>(error)
                : Task.FromResult(script.Content(context.Brief));
        }

        public Task<DraftContent> ReviseAsync(AuthoringContext context, DraftContent previous, ReviewReport review, CancellationToken cancellationToken)
        {
            script.ReviseCalls++;
            script.LastHumanFeedback = context.HumanFeedback;
            script.LastReviewSeenByWriter = string.Join('|', review.Issues.Select(i => i.Suggestion));
            return Task.FromResult(script.Content(context.Brief));
        }
    }

    private sealed class ScriptedReviewer(Script script) : ILessonReviewer
    {
        public Task<ReviewReport> ReviewAsync(AuthoringContext context, DraftContent content, CancellationToken cancellationToken)
        {
            script.ReviewCalls++;
            return Task.FromResult(script.Reviews.Dequeue());
        }
    }
}

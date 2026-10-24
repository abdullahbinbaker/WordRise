using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Application.Pipeline;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Tests;

/// <summary>Runs only when asked: these tests call the real Claude and spend a few cents.</summary>
public sealed class LiveClaudeFactAttribute : FactAttribute
{
    public LiveClaudeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("WORDRISE_LIVE_CLAUDE") != "1")
        {
            Skip = "Calls the real Claude and spends money: set WORDRISE_LIVE_CLAUDE=1 to run it.";
        }
        else if (string.IsNullOrWhiteSpace(LiveClaude.ApiKey))
        {
            Skip = "Authoring__Claude__ApiKey is not set.";
        }
    }
}

internal static class LiveClaude
{
    public static string? ApiKey => Environment.GetEnvironmentVariable("Authoring__Claude__ApiKey");
}

/// <summary>
/// Does the real reviewer catch real mistakes? A good lesson (written by Claude itself and reviewed
/// by a person) is the control; each other case is that lesson with one deliberate mistake, and the
/// reviewer must call it blocking. Nothing here is published: the writer is replaced by a fixed one,
/// and a single cycle is allowed so each case costs one review.
/// </summary>
public sealed class LiveClaudeReviewerTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private static DraftContent Lesson(Action<JsonNode>? mutate = null)
    {
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LiveReviewer", "good-lesson.json")))!;
        mutate?.Invoke(node);
        return node.Deserialize<DraftContent>(Json)!;
    }

    /// <summary>Each case: what to call it (it becomes the scenario) and the lesson to review.</summary>
    private static readonly Dictionary<string, Func<DraftContent>> Cases = new()
    {
        ["control: the good lesson"] = () => Lesson(),
        ["wrong translation"] = () => Lesson(n => n["Words"]![1]!["Native"] = "طبيب"),
        ["two correct answers"] = () => Lesson(n => n["ChatOptions"]![2]!["IsCorrect"] = true),
        ["alcohol in the dialogue"] = () => Lesson(n =>
        {
            n["Dialogue"]![0]!["Target"] = "Hello! My name is Omar. Do you want a beer?";
            n["Dialogue"]![0]!["Native"] = "مرحبًا! اسمي عمر. هل تريد بيرة؟";
        }),
        ["grammar mistake in English"] = () => Lesson(n => n["Dialogue"]![2]!["Target"] = "You is a student! I am from Amman."),
        ["grammar from a later lesson"] = () => Lesson(n =>
        {
            n["Dialogue"]![3]!["Target"] = "I went to Cairo yesterday.";
            n["Dialogue"]![3]!["Native"] = "ذهبت إلى القاهرة أمس.";
        }),
    };

    private sealed class FixedWriter : ILessonWriter
    {
        public Task<DraftContent> WriteAsync(AuthoringContext context, CancellationToken cancellationToken) =>
            Task.FromResult(Cases[context.Brief.Scenario]());

        public Task<DraftContent> ReviseAsync(AuthoringContext context, DraftContent previous, ReviewReport review, CancellationToken cancellationToken) =>
            Task.FromResult(previous);
    }

    [LiveClaudeFact]
    public async Task The_real_reviewer_accepts_a_good_lesson_and_catches_each_deliberate_mistake()
    {
        using var factory = new WordriseApiFactory { TestServices = services => services.AddScoped<ILessonWriter>(_ => new FixedWriter()) };
        factory.Settings["Authoring:Provider"] = "Claude";
        factory.Settings["Authoring:Claude:ApiKey"] = LiveClaude.ApiKey;
        factory.Settings["Authoring:MaxCycles"] = "1";
        using var staff = factory.CreateStaffClient();

        List<string> problems = [];
        foreach (var name in Cases.Keys)
        {
            var id = await AuthoringApiTests.CreateDraftAsync(staff, name, step: 1, wordCount: 5);
            for (var run = 0; run < 5 && (await AuthoringApiTests.GetAsync(staff, id))!.Status is "queued" or "inProgress"; run++)
            {
                await factory.Services.GetRequiredService<IDraftQueueRunner>().RunOnceAsync(CancellationToken.None);
            }

            var draft = (await AuthoringApiTests.GetAsync(staff, id))!;
            if (draft.Status == "failed")
            {
                problems.Add($"{name}: the run failed ({draft.Error})");
                continue;
            }

            var review = draft.Revisions.Single();
            var passed = review.GetProperty("passed").GetBoolean();
            var blocking = review.GetProperty("issues").EnumerateArray()
                .Where(issue => issue.GetProperty("severity").GetString() == "blocking")
                .Select(issue => issue.GetProperty("problem").GetString())
                .ToList();

            Console.WriteLine($"[{name}] passed={passed}, blocking issues={blocking.Count}");
            foreach (var problem in blocking)
            {
                Console.WriteLine($"    blocking: {problem}");
            }

            if (name.StartsWith("control", StringComparison.Ordinal))
            {
                if (!passed)
                {
                    problems.Add($"{name}: the good lesson was rejected ({blocking.Count} blocking issue(s))");
                }
            }
            else if (passed || blocking.Count == 0)
            {
                problems.Add($"{name}: the mistake was NOT called blocking");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }
}

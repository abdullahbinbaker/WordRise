using Microsoft.Extensions.Options;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure;

/// <summary>Which service writes and reviews the lessons.</summary>
public enum AuthoringProvider
{
    /// <summary>Claude, through Anthropic's API. Needs <c>Authoring:Claude:ApiKey</c>.</summary>
    Claude,

    /// <summary>Built-in sample lessons and a reviewer that approves everything: no key, no cost. For development.</summary>
    Sample,
}

public enum ClaudeEffort
{
    Low,
    Medium,
    High,
    Max,
}

/// <summary>The <c>Authoring</c> section of appsettings.</summary>
public sealed class AuthoringOptions
{
    public const string SectionName = "Authoring";

    public AuthoringProvider Provider { get; set; } = AuthoringProvider.Claude;

    /// <summary>Write-and-review attempts before a lesson goes to a person marked "needs attention".</summary>
    public int MaxCycles { get; set; } = 3;

    public WorkerOptions Worker { get; set; } = new();

    public ClaudeOptions Claude { get; set; } = new();
}

/// <summary><c>Authoring:Worker</c>: the background job that works through the queue of drafts.</summary>
public sealed class WorkerOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>How often the queue is checked for new drafts.</summary>
    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>Drafts taken from the queue per check; the rest wait for the next one.</summary>
    public int MaxDraftsPerPoll { get; set; } = 3;
}

/// <summary><c>Authoring:Claude</c>: used when the provider is Claude.</summary>
public sealed class ClaudeOptions
{
    /// <summary>
    /// A secret: never put it in a file that is committed. Set the environment variable
    /// <c>Authoring__Claude__ApiKey</c> on the server, or use <c>dotnet user-secrets</c> locally.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Leave empty for Anthropic's own servers; set it to go through a gateway (or a fake in tests).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Writes the lessons: the strongest model, thinking hard.</summary>
    public ClaudeModelOptions Writer { get; set; } = new() { Model = "claude-opus-5-5", Effort = ClaudeEffort.High, MaxTokens = 16000 };

    /// <summary>Reviews them: a different model on purpose, so it does not share the writer's blind spots.</summary>
    public ClaudeModelOptions Reviewer { get; set; } = new() { Model = "claude-sonnet-5-5", Effort = ClaudeEffort.Medium, MaxTokens = 8000 };
}

public sealed class ClaudeModelOptions
{
    public string Model { get; set; } = string.Empty;

    public ClaudeEffort Effort { get; set; } = ClaudeEffort.Medium;

    /// <summary>Most tokens in one answer, thinking included.</summary>
    public int MaxTokens { get; set; } = 16000;
}

/// <summary>
/// Refuses out-of-range settings at startup, so a typo is found when the app starts and not
/// in the middle of the night. A missing API key is not checked here: it must not take down the
/// learners' side, so drafts fail with a clear message instead.
/// </summary>
internal sealed class AuthoringOptionsValidator : IValidateOptions<AuthoringOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthoringOptions options)
    {
        List<string> failures = [];

        if (options.MaxCycles is < 1 or > 10)
        {
            failures.Add("Authoring:MaxCycles must be 1 to 10.");
        }

        if (options.Worker.PollIntervalSeconds is < 5 or > 3600)
        {
            failures.Add("Authoring:Worker:PollIntervalSeconds must be 5 to 3600.");
        }

        if (options.Worker.MaxDraftsPerPoll is < 1 or > 50)
        {
            failures.Add("Authoring:Worker:MaxDraftsPerPoll must be 1 to 50.");
        }

        CheckModel(options.Claude.Writer, "Authoring:Claude:Writer", failures);
        CheckModel(options.Claude.Reviewer, "Authoring:Claude:Reviewer", failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void CheckModel(ClaudeModelOptions model, string path, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(model.Model))
        {
            failures.Add($"{path}:Model must not be empty.");
        }

        if (model.MaxTokens is < 1000 or > 64000)
        {
            failures.Add($"{path}:MaxTokens must be 1000 to 64000.");
        }
    }
}

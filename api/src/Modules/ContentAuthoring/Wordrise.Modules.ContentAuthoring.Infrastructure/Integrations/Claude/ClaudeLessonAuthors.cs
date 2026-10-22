using Microsoft.Extensions.Options;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Claude;

/// <summary>Writes lessons with Claude (Opus by default): the strongest model, thinking hard.</summary>
internal sealed class ClaudeLessonWriter(ClaudeJsonClient claude, IOptions<AuthoringOptions> options) : ILessonWriter
{
    public async Task<DraftContent> WriteAsync(AuthoringContext context, CancellationToken cancellationToken)
    {
        var lesson = await claude.AskAsync<LessonJson>(
            options.Value.Claude.Writer,
            ClaudePrompts.WriterSystem,
            ClaudePrompts.WriteRequest(context),
            ClaudeSchemas.Lesson,
            cancellationToken);
        return lesson.ToDomain();
    }

    public async Task<DraftContent> ReviseAsync(
        AuthoringContext context,
        DraftContent previous,
        ReviewReport review,
        CancellationToken cancellationToken)
    {
        var lesson = await claude.AskAsync<LessonJson>(
            options.Value.Claude.Writer,
            ClaudePrompts.WriterSystem,
            ClaudePrompts.ReviseRequest(context, previous, review),
            ClaudeSchemas.Lesson,
            cancellationToken);
        return lesson.ToDomain();
    }
}

/// <summary>
/// Reviews lessons with Claude (Sonnet by default): a different model from the writer on purpose,
/// told only to find mistakes, so it does not share the writer's blind spots.
/// </summary>
internal sealed class ClaudeLessonReviewer(ClaudeJsonClient claude, IOptions<AuthoringOptions> options) : ILessonReviewer
{
    public async Task<ReviewReport> ReviewAsync(AuthoringContext context, DraftContent content, CancellationToken cancellationToken)
    {
        var review = await claude.AskAsync<ReviewJson>(
            options.Value.Claude.Reviewer,
            ClaudePrompts.ReviewerSystem,
            ClaudePrompts.ReviewRequest(context, content),
            ClaudeSchemas.Review,
            cancellationToken);
        return review.ToDomain();
    }
}

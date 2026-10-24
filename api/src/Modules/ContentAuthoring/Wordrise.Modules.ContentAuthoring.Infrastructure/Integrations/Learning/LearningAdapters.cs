using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Shared.Kernel;
using LearningContracts = Wordrise.Modules.Learning.Contracts;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Learning;

/// <summary>
/// The only place authoring touches the Learning module, and only through its public contracts:
/// the lesson shape is translated here, so neither module knows the other's internals.
/// </summary>
internal sealed class LearningLessonPublisher(LearningContracts.ILessonPublisher learning) : ILessonPublisher
{
    public async Task<Result<PublishedLesson>> PublishAsync(
        Guid draftId,
        LessonBrief brief,
        DraftContent content,
        CancellationToken cancellationToken)
    {
        // The draft id is the lesson id: publishing the same draft again finds its lesson and changes nothing.
        var request = new LearningContracts.PublishLessonRequest(
            draftId,
            brief.TargetLanguage,
            brief.NativeLanguage,
            brief.GrammarStep,
            content.Title,
            brief.Scenario,
            brief.Grammar.Title,
            [.. content.Words.Select(w => new LearningContracts.PublishedWord(w.Target, w.Native, w.Pronunciation, w.Example, w.ExampleNative))],
            [.. content.Dialogue.Select(d => new LearningContracts.PublishedDialogueLine(d.Speaker, d.IsLearner, d.Target, d.Native))],
            content.ChatPrompt,
            [.. content.ChatOptions.Select(o => new LearningContracts.PublishedAnswerOption(o.Text, o.IsCorrect))],
            new LearningContracts.PublishedGrammar(
                content.Grammar.Title,
                content.Grammar.Meaning,
                content.Grammar.Explanation,
                content.Grammar.Pattern,
                [.. content.Grammar.Examples.Select(e => new LearningContracts.PublishedGrammarExample(e.Target, e.Native))],
                content.Grammar.Note),
            content.ListeningSentence,
            [.. content.ListeningOptions.Select(o => new LearningContracts.PublishedAnswerOption(o.Text, o.IsCorrect))]);

        try
        {
            var published = await learning.PublishAsync(request, cancellationToken);
            return new PublishedLesson(published.LessonId, published.Number);
        }
        catch (BusinessRuleException rule)
        {
            // Learning protects itself: a lesson its learners could not complete is refused.
            return Error.Validation(AuthoringErrors.PublishRejected, $"The lesson was not published: {rule.Message} ({rule.Code})");
        }
    }
}

internal sealed class LearningKnownVocabulary(LearningContracts.IPublishedVocabulary learning) : IKnownVocabulary
{
    public Task<IReadOnlyList<string>> GetTaughtWordsAsync(string targetLanguage, CancellationToken cancellationToken) =>
        learning.GetTaughtWordsAsync(targetLanguage, cancellationToken);
}

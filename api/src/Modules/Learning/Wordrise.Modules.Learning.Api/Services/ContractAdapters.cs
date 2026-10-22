using Mediator;
using Wordrise.Modules.Learning.Application.Features.Lessons;
using Wordrise.Modules.Learning.Application.Features.Words;
using Wordrise.Modules.Learning.Contracts;

namespace Wordrise.Modules.Learning.Api.Services;

/// <summary>Lets other modules publish lessons, through the same command the module uses itself.</summary>
internal sealed class LessonPublisherAdapter(IMediator mediator) : ILessonPublisher
{
    public async Task<PublishedLessonInfo> PublishAsync(PublishLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishLessonCommand(request), cancellationToken);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(result.Error!.Message);
    }
}

/// <summary>Lets other modules read which words learners were taught.</summary>
internal sealed class PublishedVocabularyAdapter(IMediator mediator) : IPublishedVocabulary
{
    public async Task<IReadOnlyList<string>> GetTaughtWordsAsync(string targetLanguage, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaughtWordsQuery(targetLanguage), cancellationToken);
        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException(result.Error!.Message);
    }
}

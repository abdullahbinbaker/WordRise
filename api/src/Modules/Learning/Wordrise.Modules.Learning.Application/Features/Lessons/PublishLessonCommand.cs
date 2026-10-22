using Mediator;
using Microsoft.Extensions.Logging;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Contracts;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Media.Contracts;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>
/// Adds a finished lesson to the course, as the next one. Repeating it for the same lesson id
/// changes nothing, so a caller can retry after a failure without publishing twice.
/// </summary>
public sealed record PublishLessonCommand(PublishLessonRequest Request) : ICommand<Result<PublishedLessonInfo>>;

public sealed class PublishLessonCommandHandler(
    ILessonRepository lessons,
    IUnitOfWork unitOfWork,
    IAudioLibrary audio,
    ILogger<PublishLessonCommandHandler> logger)
    : ICommandHandler<PublishLessonCommand, Result<PublishedLessonInfo>>
{
    public async ValueTask<Result<PublishedLessonInfo>> Handle(
        PublishLessonCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (await lessons.GetNumberAsync(request.LessonId, cancellationToken) is { } existingNumber)
        {
            return new PublishedLessonInfo(request.LessonId, existingNumber, AlreadyPublished: true);
        }

        var content = new LessonContent
        {
            Words =
            [
                .. request.Words.Select(w => new LessonWord
                {
                    Id = WordIdentity.For(request.TargetLanguage, w.Target),
                    Target = w.Target.Trim(),
                    Native = w.Native.Trim(),
                    Pronunciation = w.Pronunciation.Trim(),
                    Example = w.Example.Trim(),
                    ExampleNative = w.ExampleNative.Trim(),
                }),
            ],
            Dialogue =
            [
                .. request.Dialogue.Select(d => new DialogueLine
                {
                    Speaker = d.Speaker,
                    IsLearner = d.IsLearner,
                    Target = d.Target,
                    Native = d.Native,
                }),
            ],
            ChatPrompt = request.ChatPrompt,
            ChatOptions = [.. request.ChatOptions.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect })],
            Grammar = new GrammarExplanation
            {
                Title = request.Grammar.Title,
                Meaning = request.Grammar.Meaning,
                Explanation = request.Grammar.Explanation,
                Pattern = request.Grammar.Pattern,
                Examples = [.. request.Grammar.Examples.Select(e => new GrammarExample { Target = e.Target, Native = e.Native })],
                Note = request.Grammar.Note,
            },
            ListeningSentence = request.ListeningSentence,
            ListeningOptions = [.. request.ListeningOptions.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect })],
        };

        var number = await lessons.NextNumberAsync(cancellationToken);
        var lesson = Lesson.Create(request.LessonId, number, request.TargetLanguage, request.NativeLanguage, request.Title, request.Scenario, request.GrammarPoint, content);
        lessons.Add(lesson);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await RecordAudioAsync(lesson, cancellationToken);

        return new PublishedLessonInfo(lesson.Id, lesson.Number, AlreadyPublished: false);
    }

    /// <summary>
    /// The lesson is already saved, so a speech outage must not undo the publication: the app
    /// speaks with the device's voice until the audio is made (see <see cref="GenerateLessonAudioCommand"/>).
    /// </summary>
    private async Task RecordAudioAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        try
        {
            var result = await audio.EnsureAsync(LessonSpeech.Collect(lesson), cancellationToken);
            if (result.Failed > 0)
            {
                logger.LogWarning("Lesson {Number}: {Failed} of {Requested} recordings failed", lesson.Number, result.Failed, result.Requested);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Lesson {Number} was published without audio", lesson.Number);
        }
    }
}

using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Contracts;
using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>
/// Adds a finished lesson to the course, as the next one. Repeating it for the same lesson id
/// changes nothing, so a caller can retry after a failure without publishing twice.
/// </summary>
public sealed record PublishLessonCommand(PublishLessonRequest Request) : ICommand<Result<PublishedLessonInfo>>;

public sealed class PublishLessonCommandHandler(ILessonRepository lessons, IUnitOfWork unitOfWork)
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
                    Id = WordIdentity.For(w.English),
                    English = w.English.Trim(),
                    Arabic = w.Arabic.Trim(),
                    Pronunciation = w.Pronunciation.Trim(),
                    Example = w.Example.Trim(),
                    ExampleArabic = w.ExampleArabic.Trim(),
                }),
            ],
            Dialogue =
            [
                .. request.Dialogue.Select(d => new DialogueLine
                {
                    Speaker = d.Speaker,
                    IsLearner = d.IsLearner,
                    English = d.English,
                    Arabic = d.Arabic,
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
                Examples = [.. request.Grammar.Examples.Select(e => new GrammarExample { English = e.English, Arabic = e.Arabic })],
                Note = request.Grammar.Note,
            },
            ListeningSentence = request.ListeningSentence,
            ListeningOptions = [.. request.ListeningOptions.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect })],
        };

        var number = await lessons.NextNumberAsync(cancellationToken);
        var lesson = Lesson.Create(request.LessonId, number, request.Title, request.Scenario, request.GrammarPoint, content);
        lessons.Add(lesson);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PublishedLessonInfo(lesson.Id, lesson.Number, AlreadyPublished: false);
    }
}

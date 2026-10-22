using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain;
using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>
/// Finishing a lesson adds its words to the learner's reviews and counts as a study day.
/// Refused while the review backlog pauses new lessons.
/// </summary>
public sealed record CompleteLessonCommand(Guid LessonId) : ICommand<Result<CompleteLessonDto>>;

public sealed class CompleteLessonCommandHandler(
    ILessonRepository lessons,
    ILearnerRepository learners,
    IWordProgressRepository words,
    IUnitOfWork unitOfWork,
    ICurrentLearner currentLearner,
    ILearningCalendar calendar,
    ILearningSettings settings) : ICommandHandler<CompleteLessonCommand, Result<CompleteLessonDto>>
{
    public async ValueTask<Result<CompleteLessonDto>> Handle(
        CompleteLessonCommand command,
        CancellationToken cancellationToken)
    {
        if (await lessons.GetAsync(command.LessonId, cancellationToken) is not { } lesson)
        {
            return Error.NotFound(LearningErrors.LessonNotFound, "Lesson not found.");
        }

        var today = calendar.Today;
        var learner = await learners.GetOrStartAsync(currentLearner.Id, cancellationToken);
        var dueReviewCount = await words.CountDueAsync(learner.Id, today, cancellationToken);

        if (!learner.CompleteLesson(lesson.Id, today, dueReviewCount, settings.ReviewBacklog))
        {
            return new CompleteLessonDto(AlreadyCompleted: true, WordsAdded: 0, Streak: learner.StreakOn(today));
        }

        // A word can appear in more than one lesson; it is only introduced once.
        var known = await words.GetKnownWordIdsAsync(
            learner.Id,
            [.. lesson.Content.Words.Select(w => w.Id)],
            cancellationToken);
        var introduced = lesson.Content.Words
            .Where(word => !known.Contains(word.Id))
            .Select(word => WordProgress.Introduce(learner.Id, lesson.Id, word, today))
            .ToList();
        words.AddRange(introduced);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompleteLessonDto(AlreadyCompleted: false, introduced.Count, learner.StreakOn(today));
    }
}

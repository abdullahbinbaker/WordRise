using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain;
using Wordrise.Modules.Learning.Domain.Vocabulary;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Reviews;

/// <summary>The learner says how hard a word was to remember; that sets its next review.</summary>
public sealed record RateWordCommand(Guid WordId, ReviewRating Rating) : ICommand<Result<RateWordDto>>;

public sealed class RateWordCommandHandler(
    IWordProgressRepository words,
    ILearnerRepository learners,
    IUnitOfWork unitOfWork,
    ICurrentLearner currentLearner,
    ILearningCalendar calendar,
    ILearningSettings settings) : ICommandHandler<RateWordCommand, Result<RateWordDto>>
{
    public async ValueTask<Result<RateWordDto>> Handle(RateWordCommand command, CancellationToken cancellationToken)
    {
        if (!settings.ReviewsEnabled)
        {
            return Error.Conflict(LearningErrors.ReviewsDisabled, "Reviews are switched off.");
        }

        if (await words.GetAsync(currentLearner.Id, command.WordId, cancellationToken) is not { } progress)
        {
            return Error.NotFound(LearningErrors.WordNotFound, "This word is not in your reviews.");
        }

        var today = calendar.Today;
        progress.Review(command.Rating, today);

        var learner = await learners.GetOrStartAsync(currentLearner.Id, cancellationToken);
        learner.RecordStudyDay(today);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RateWordDto(progress.WordId, progress.IntervalDays, progress.DueOn, progress.StatusOn(today));
    }
}

using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain.Learners;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Goal;

public sealed record SetDailyGoalCommand(int WordsPerDay) : ICommand<Result>;

public sealed class SetDailyGoalCommandHandler(
    ILearnerRepository learners,
    IUnitOfWork unitOfWork,
    ICurrentLearner currentLearner) : ICommandHandler<SetDailyGoalCommand, Result>
{
    public async ValueTask<Result> Handle(SetDailyGoalCommand command, CancellationToken cancellationToken)
    {
        var learner = await learners.GetOrStartAsync(currentLearner.Id, cancellationToken);

        learner.SetDailyGoal(command.WordsPerDay);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

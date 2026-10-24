using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Onboarding;

public sealed record GetOnboardingQuery : IQuery<Result<OnboardingDto>>;

public sealed class GetOnboardingQueryHandler(ILearningQueries queries, ICurrentLearner learner)
    : IQueryHandler<GetOnboardingQuery, Result<OnboardingDto>>
{
    public async ValueTask<Result<OnboardingDto>> Handle(GetOnboardingQuery query, CancellationToken cancellationToken) =>
        await queries.GetOnboardingAsync(learner.Id, cancellationToken);
}

/// <summary>Saves the answers given at sign-up. May be repeated: the learner can change their mind.</summary>
public sealed record SetOnboardingCommand(bool KnowsLetters, int StartStep) : ICommand<Result>;

public sealed class SetOnboardingCommandHandler(
    ILearnerRepository learners,
    IUnitOfWork unitOfWork,
    ICurrentLearner currentLearner) : ICommandHandler<SetOnboardingCommand, Result>
{
    public async ValueTask<Result> Handle(SetOnboardingCommand command, CancellationToken cancellationToken)
    {
        var learner = await learners.GetOrStartAsync(currentLearner.Id, cancellationToken);

        learner.ChooseStart(command.KnowsLetters, command.StartStep);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

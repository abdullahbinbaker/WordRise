namespace Wordrise.Modules.Learning.Application.Features.Onboarding;

/// <summary>What the learner told us at sign-up, and the choices on offer.</summary>
/// <param name="KnowsLetters">False: the learner starts with the foundations course.</param>
/// <param name="StartStep">The grammar step the main course starts from.</param>
/// <param name="Done">True once the learner answered the questions.</param>
public sealed record OnboardingDto(
    bool KnowsLetters,
    int StartStep,
    bool Done,
    IReadOnlyList<StartingPointDto> StartingPoints);

public sealed record StartingPointDto(int Step, string Title, string Example);

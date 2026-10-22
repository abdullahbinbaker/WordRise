namespace Wordrise.Modules.Learning.Application.Features.Goal;

public sealed record DailyGoalDto(int WordsPerDay, IReadOnlyList<int> AllowedValues);

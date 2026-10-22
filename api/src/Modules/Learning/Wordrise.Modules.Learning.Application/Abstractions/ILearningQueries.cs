using Wordrise.Modules.Learning.Application.Features.Goal;
using Wordrise.Modules.Learning.Application.Features.Lessons;
using Wordrise.Modules.Learning.Application.Features.Reviews;
using Wordrise.Modules.Learning.Application.Features.Today;
using Wordrise.Modules.Learning.Application.Features.Words;

namespace Wordrise.Modules.Learning.Application.Abstractions;

/// <summary>Read side: queries return screen-ready data without going through the aggregates.</summary>
public interface ILearningQueries
{
    Task<DailyGoalDto> GetDailyGoalAsync(Guid learnerId, CancellationToken cancellationToken);

    Task<TodayDto> GetTodayAsync(Guid learnerId, DateOnly today, CancellationToken cancellationToken);

    Task<LessonDto?> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetTaughtWordsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DueReviewDto>> GetDueReviewsAsync(
        Guid learnerId,
        DateOnly today,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MyWordDto>> ListWordsAsync(
        Guid learnerId,
        WordFilter filter,
        DateOnly today,
        CancellationToken cancellationToken);
}

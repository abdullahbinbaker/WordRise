using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Application.Features.Reviews;

public sealed record DueReviewDto(Guid WordId, string English, string Arabic);

public sealed record RateWordDto(Guid WordId, int IntervalDays, DateOnly NextReviewOn, WordStatus Status);

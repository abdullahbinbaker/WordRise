using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Application.Features.Reviews;

public sealed record DueReviewDto(Guid WordId, string Target, string Native);

public sealed record RateWordDto(Guid WordId, int IntervalDays, DateOnly NextReviewOn, WordStatus Status);

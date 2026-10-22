using Wordrise.Modules.Learning.Domain.Vocabulary;

namespace Wordrise.Modules.Learning.Application.Features.Words;

public enum WordFilter
{
    All,
    Due,
    Mastered,
}

public sealed record MyWordDto(Guid WordId, string English, string Arabic, WordStatus Status, DateOnly NextReviewOn);

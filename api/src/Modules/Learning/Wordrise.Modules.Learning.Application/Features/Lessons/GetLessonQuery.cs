using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Learning.Domain;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

public sealed record GetLessonQuery(Guid LessonId) : IQuery<Result<LessonDto>>;

public sealed class GetLessonQueryHandler(ILearningQueries queries) : IQueryHandler<GetLessonQuery, Result<LessonDto>>
{
    public async ValueTask<Result<LessonDto>> Handle(GetLessonQuery query, CancellationToken cancellationToken) =>
        await queries.GetLessonAsync(query.LessonId, cancellationToken) is { } lesson
            ? lesson
            : Error.NotFound(LearningErrors.LessonNotFound, "Lesson not found.");
}

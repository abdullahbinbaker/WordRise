using Mediator;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Domain;
using Wordrise.Shared.Kernel;

namespace Wordrise.Modules.ContentAuthoring.Application.Features.Drafts;

/// <summary>Asks for a new lesson: the writer picks it up from the queue.</summary>
public sealed record CreateDraftCommand(string? Scenario, int GrammarStep, int? WordCount) : ICommand<Result<Guid>>;

public sealed class CreateDraftCommandHandler(
    IDraftRepository drafts,
    IUnitOfWork unitOfWork,
    ICurrentStaff staff,
    TimeProvider clock) : ICommandHandler<CreateDraftCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateDraftCommand command, CancellationToken cancellationToken)
    {
        var brief = LessonBrief.Create(command.Scenario, command.GrammarStep, command.WordCount);
        var draft = LessonDraft.Create(Guid.CreateVersion7(), brief, staff.Id, clock.GetUtcNow().UtcDateTime);

        drafts.Add(draft);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return draft.Id;
    }
}

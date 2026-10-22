using Mediator;
using Wordrise.Modules.Learning.Application.Abstractions;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>
/// Records the audio of every published lesson that is missing some: for lessons published before
/// a speech provider was set up, or after one recording failed. Safe to repeat.
/// </summary>
public sealed record GenerateLessonAudioCommand : ICommand<AudioGenerationResult>;

public sealed class GenerateLessonAudioCommandHandler(ILessonRepository lessons, IAudioLibrary audio)
    : ICommandHandler<GenerateLessonAudioCommand, AudioGenerationResult>
{
    public async ValueTask<AudioGenerationResult> Handle(GenerateLessonAudioCommand command, CancellationToken cancellationToken)
    {
        var texts = (await lessons.ListAsync(cancellationToken)).SelectMany(l => LessonSpeech.Collect(l.Content)).ToList();
        return await audio.EnsureAsync(texts, cancellationToken);
    }
}

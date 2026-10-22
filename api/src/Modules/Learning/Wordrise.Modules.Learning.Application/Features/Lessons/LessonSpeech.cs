using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>Every English text a lesson lets the learner listen to.</summary>
public static class LessonSpeech
{
    public static IReadOnlyList<SpokenText> Collect(LessonContent content) =>
    [
        .. content.Words.Select(w => new SpokenText(w.English)),
        .. content.Words.Select(w => new SpokenText(w.Example)),
        // The other person in a dialogue has a different voice from the learner's own lines.
        .. content.Dialogue.Select(d => new SpokenText(d.English, d.IsLearner ? SpeakerVoice.Primary : SpeakerVoice.Secondary)),
        .. content.Grammar.Examples.Select(e => new SpokenText(e.English)),
        .. content.ChatOptions.Select(o => new SpokenText(o.Text)),
        new SpokenText(content.ListeningSentence),
        new SpokenText(content.ListeningSentence, Slow: true),
    ];
}

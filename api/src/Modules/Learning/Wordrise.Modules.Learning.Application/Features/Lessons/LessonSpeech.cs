using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>Every text, in the language being taught, that a lesson lets the learner listen to.</summary>
public static class LessonSpeech
{
    public static IReadOnlyList<SpokenText> Collect(Lesson lesson)
    {
        var language = lesson.TargetLanguage;
        var content = lesson.Content;
        return
        [
            .. content.Words.Select(w => new SpokenText(w.Target, language)),
            .. content.Words.Select(w => new SpokenText(w.Example, language)),
            // The other person in a dialogue has a different voice from the learner's own lines.
            .. content.Dialogue.Select(d => new SpokenText(d.Target, language, d.IsLearner ? SpeakerVoice.Primary : SpeakerVoice.Secondary)),
            .. content.Grammar.Examples.Select(e => new SpokenText(e.Target, language)),
            .. content.ChatOptions.Select(o => new SpokenText(o.Text, language)),
            new SpokenText(content.ListeningSentence, language),
            new SpokenText(content.ListeningSentence, language, Slow: true),
        ];
    }
}

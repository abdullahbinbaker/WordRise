using Wordrise.Modules.Learning.Domain.Lessons;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Learning.Application.Features.Lessons;

/// <summary>Every text, in the language being taught, that a lesson lets the learner listen to.</summary>
public static class LessonSpeech
{
    public static IReadOnlyList<SpokenText> Collect(Lesson lesson) =>
        lesson.Track == LessonTrack.Foundations ? Foundations(lesson) : Main(lesson);

    private static IReadOnlyList<SpokenText> Main(Lesson lesson)
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

    /// <summary>
    /// A letter is spoken twice, by its name and by its sound (the web app asks for the sound of the
    /// lower-case letter); other items and the exercises' audio are spoken as written.
    /// </summary>
    private static IReadOnlyList<SpokenText> Foundations(Lesson lesson)
    {
        var language = lesson.TargetLanguage;
        var content = lesson.Content;
        List<SpokenText> texts = [];

        foreach (var item in content.Words)
        {
            if (item.IsLetter)
            {
                texts.Add(new SpokenText(item.Target, language, Style: SpeechStyle.Letter));
                texts.Add(new SpokenText(item.Target.ToLowerInvariant(), language, Style: SpeechStyle.Sound, Ipa: item.Ipa));
            }
            else
            {
                texts.Add(new SpokenText(item.Target, language));
            }

            if (!string.IsNullOrWhiteSpace(item.Example))
            {
                texts.Add(new SpokenText(item.Example, language));
            }
        }

        foreach (var exercise in content.Exercises.Where(e => !string.IsNullOrWhiteSpace(e.AudioText)))
        {
            texts.Add(new SpokenText(
                exercise.AudioText,
                language,
                Style: exercise.AudioStyle switch
                {
                    SpokenStyle.Letter => SpeechStyle.Letter,
                    SpokenStyle.Sound => SpeechStyle.Sound,
                    _ => SpeechStyle.Normal,
                },
                Ipa: string.IsNullOrWhiteSpace(exercise.AudioIpa) ? null : exercise.AudioIpa));
        }

        return texts;
    }
}

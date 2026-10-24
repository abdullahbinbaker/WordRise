using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Wordrise.Modules.Media.Contracts;

/// <summary>Which of the two voices reads a text: dialogue partners sound different from each other.</summary>
public enum SpeakerVoice
{
    Primary,
    Secondary,
}

/// <summary>How a text is spoken. A letter on its own would be read as a word, so letters need their own styles.</summary>
public enum SpeechStyle
{
    /// <summary>An ordinary word or sentence.</summary>
    Normal,

    /// <summary>The name of a letter: "A" is read "ay".</summary>
    Letter,

    /// <summary>The sound a letter makes: "A" is read "a" as in apple. Needs <see cref="SpokenText.Ipa"/>.</summary>
    Sound,
}

/// <summary>A text that learners will want to hear.</summary>
/// <param name="Language">ISO code of the language the text is in, for example en.</param>
/// <param name="Slow">The slow, clearly pronounced version used by listening exercises.</param>
/// <param name="Style">Normal speech, a letter's name, or a letter's sound.</param>
/// <param name="Ipa">The sound in the International Phonetic Alphabet, required for <see cref="SpeechStyle.Sound"/>.</param>
public sealed record SpokenText(
    string Text,
    string Language,
    SpeakerVoice Voice = SpeakerVoice.Primary,
    bool Slow = false,
    SpeechStyle Style = SpeechStyle.Normal,
    string? Ipa = null);

/// <param name="Enabled">False when no speech provider is configured: nothing was attempted.</param>
public sealed record AudioGenerationResult(bool Enabled, int Requested, int Created, int AlreadyThere, int Failed);

/// <summary>
/// The audio of every spoken text, made once and kept. Callers ask for the texts they need and
/// never wait for audio to be played: a missing recording only means the app speaks with the
/// device's own voice.
/// </summary>
public interface IAudioLibrary
{
    /// <summary>Makes the recordings that do not exist yet. One text failing does not stop the others.</summary>
    Task<AudioGenerationResult> EnsureAsync(IReadOnlyCollection<SpokenText> texts, CancellationToken cancellationToken);
}

/// <summary>
/// The name of a recording, worked out from the text alone so the website can find it without a
/// lookup. The web app computes the same value (<c>audio-id.ts</c>); change both together.
/// </summary>
public static partial class AudioId
{
    private const string Prefix = "wordrise:audio:v2:";

    public static string Normalize(string text) => Whitespace().Replace(text, " ").Trim();

    /// <summary>The style appears in the name only when it is not Normal, so ordinary recordings keep their names.</summary>
    public static string For(string language, string text, bool slow = false, SpeechStyle style = SpeechStyle.Normal)
    {
        var styleKey = style switch
        {
            SpeechStyle.Letter => "letter:",
            SpeechStyle.Sound => "sound:",
            _ => string.Empty,
        };
        var key = Prefix + language.Trim().ToLowerInvariant() + ":" + (slow ? "slow:" : string.Empty) + styleKey + Normalize(text);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

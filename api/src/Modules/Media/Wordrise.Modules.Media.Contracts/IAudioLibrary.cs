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

/// <summary>A text that learners will want to hear.</summary>
/// <param name="Language">ISO code of the language the text is in, for example en.</param>
/// <param name="Slow">The slow, clearly pronounced version used by listening exercises.</param>
public sealed record SpokenText(string Text, string Language, SpeakerVoice Voice = SpeakerVoice.Primary, bool Slow = false);

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

    public static string For(string language, string text, bool slow = false)
    {
        var key = Prefix + language.Trim().ToLowerInvariant() + ":" + (slow ? "slow:" : string.Empty) + Normalize(text);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

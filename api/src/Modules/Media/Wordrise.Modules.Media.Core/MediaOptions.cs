using Microsoft.Extensions.Options;

namespace Wordrise.Modules.Media.Core;

internal enum SpeechProvider
{
    /// <summary>No recordings are made; the app speaks with the device's own voice.</summary>
    None,

    Azure,
}

internal sealed class MediaOptions
{
    public const string SectionName = "Media";

    public SpeechProvider Provider { get; set; } = SpeechProvider.None;

    public AzureSpeechOptions Azure { get; set; } = new();

    public StorageOptions Storage { get; set; } = new();
}

internal sealed class AzureSpeechOptions
{
    /// <summary>Never in a file: set <c>Media__Azure__Key</c> on the server (or use user-secrets).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The region of the Speech resource, for example <c>westeurope</c>.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Overrides the service address; only tests use it.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>The voices to use for each language, by ISO code. Teaching a new language starts here.</summary>
    public Dictionary<string, LanguageVoices> Voices { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new() { Locale = "en-US", Primary = "en-US-JennyNeural", Secondary = "en-US-GuyNeural" },
    };

    /// <summary>How much slower the slow version speaks, as SSML prosody rate.</summary>
    public string SlowRate { get; set; } = "-40%";

    /// <summary>Synthesis requests running at the same time.</summary>
    public int MaxParallelRequests { get; set; } = 3;
}

internal sealed class LanguageVoices
{
    /// <summary>The locale of the voices, for example en-US.</summary>
    public string Locale { get; set; } = string.Empty;

    /// <summary>Reads words and the learner's own lines.</summary>
    public string Primary { get; set; } = string.Empty;

    /// <summary>Reads the other person in a dialogue.</summary>
    public string Secondary { get; set; } = string.Empty;
}

internal sealed class StorageOptions
{
    /// <summary>Where recordings are kept: absolute, or relative to the app's folder.</summary>
    public string Path { get; set; } = "media-data";
}

internal sealed class MediaOptionsValidator : IValidateOptions<MediaOptions>
{
    public ValidateOptionsResult Validate(string? name, MediaOptions options)
    {
        List<string> failures = [];

        if (options.Azure.MaxParallelRequests is < 1 or > 10)
        {
            failures.Add("Media:Azure:MaxParallelRequests must be between 1 and 10.");
        }

        if (string.IsNullOrWhiteSpace(options.Storage.Path))
        {
            failures.Add("Media:Storage:Path is required.");
        }

        foreach (var (language, voices) in options.Azure.Voices)
        {
            if (string.IsNullOrWhiteSpace(voices.Locale) || string.IsNullOrWhiteSpace(voices.Primary) || string.IsNullOrWhiteSpace(voices.Secondary))
            {
                failures.Add($"Media:Azure:Voices:{language} needs Locale, Primary and Secondary.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.Azure.SlowRate))
        {
            failures.Add("Media:Azure:SlowRate is required, for example -40%.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}

using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Media.Core.Audio;

/// <summary>Turns an English text into spoken audio (MP3).</summary>
internal interface ISpeechSynthesizer
{
    /// <summary>False when no provider is set up, so callers skip the work instead of failing.</summary>
    bool IsConfigured { get; }

    Task<byte[]> SynthesizeAsync(SpokenText text, CancellationToken cancellationToken);
}

/// <summary>Azure AI Speech over its REST API: a short SSML request in, an MP3 out. No SDK needed.</summary>
internal sealed class AzureSpeechSynthesizer(HttpClient http, IOptions<MediaOptions> options) : ISpeechSynthesizer
{
    private const string OutputFormat = "audio-24khz-48kbitrate-mono-mp3";

    private MediaOptions Settings => options.Value;

    public bool IsConfigured =>
        Settings.Provider == SpeechProvider.Azure
        && !string.IsNullOrWhiteSpace(Settings.Azure.Key)
        && (!string.IsNullOrWhiteSpace(Settings.Azure.Region) || !string.IsNullOrWhiteSpace(Settings.Azure.Endpoint));

    public async Task<byte[]> SynthesizeAsync(SpokenText text, CancellationToken cancellationToken)
    {
        var azure = Settings.Azure;
        var endpoint = string.IsNullOrWhiteSpace(azure.Endpoint)
            ? $"https://{azure.Region}.tts.speech.microsoft.com/cognitiveservices/v1"
            : azure.Endpoint;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(Ssml(text, azure), Encoding.UTF8, new MediaTypeHeaderValue("application/ssml+xml")),
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", azure.Key);
        request.Headers.Add("X-Microsoft-OutputFormat", OutputFormat);
        request.Headers.UserAgent.ParseAdd("Wordrise");

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Azure Speech answered {(int)response.StatusCode} {response.ReasonPhrase}. Check Media:Azure:Key and Region.");
        }

        var audio = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return audio.Length > 0 ? audio : throw new InvalidOperationException("Azure Speech returned an empty recording.");
    }

    internal static string Ssml(SpokenText text, AzureSpeechOptions azure)
    {
        var voice = text.Voice == SpeakerVoice.Secondary ? azure.SecondaryVoice : azure.PrimaryVoice;
        var spoken = SecurityElement.Escape(AudioId.Normalize(text.Text));
        var body = text.Slow ? $"<prosody rate=\"{SecurityElement.Escape(azure.SlowRate)}\">{spoken}</prosody>" : spoken;
        return $"<speak version=\"1.0\" xml:lang=\"en-US\"><voice name=\"{SecurityElement.Escape(voice)}\">{body}</voice></speak>";
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Media.Contracts;

namespace Wordrise.Modules.Media.Core.Audio;

internal sealed class AudioLibrary(
    ISpeechSynthesizer synthesizer,
    IAudioStore store,
    IOptions<MediaOptions> options,
    ILogger<AudioLibrary> logger) : IAudioLibrary
{
    public async Task<AudioGenerationResult> EnsureAsync(
        IReadOnlyCollection<SpokenText> texts,
        CancellationToken cancellationToken)
    {
        // The same sentence often appears in several places; it is recorded once.
        var wanted = texts
            .Where(t => !string.IsNullOrWhiteSpace(t.Text))
            .GroupBy(t => AudioId.For(t.Language, t.Text, t.Slow, t.Style))
            .ToDictionary(group => group.Key, group => group.First());

        if (!synthesizer.IsConfigured)
        {
            return new AudioGenerationResult(Enabled: false, wanted.Count, 0, 0, 0);
        }

        var missing = wanted.Where(pair => !store.Exists(pair.Key)).ToList();
        var created = 0;
        var failed = 0;

        await Parallel.ForEachAsync(
            missing,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.Value.Azure.MaxParallelRequests,
                CancellationToken = cancellationToken,
            },
            async (pair, token) =>
            {
                try
                {
                    var audio = await synthesizer.SynthesizeAsync(pair.Value, token);
                    await store.SaveAsync(pair.Key, audio, token);
                    Interlocked.Increment(ref created);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    Interlocked.Increment(ref failed);
                    logger.LogWarning(exception, "Could not record \"{Text}\"", pair.Value.Text);
                }
            });

        return new AudioGenerationResult(Enabled: true, wanted.Count, created, wanted.Count - missing.Count, failed);
    }
}

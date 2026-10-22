using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Wordrise.Modules.Media.Core.Audio;

internal sealed record StoredAudio(Stream Content, DateTime LastModifiedUtc, long Length);

/// <summary>Where finished recordings are kept. A folder today; cloud storage later without touching callers.</summary>
internal interface IAudioStore
{
    bool Exists(string id);

    /// <summary>The recording, or null when it does not exist. The caller disposes the stream.</summary>
    StoredAudio? Open(string id);

    Task SaveAsync(string id, byte[] audio, CancellationToken cancellationToken);
}

internal sealed class FolderAudioStore : IAudioStore
{
    private readonly string _folder;

    public FolderAudioStore(IOptions<MediaOptions> options, IHostEnvironment environment)
    {
        var path = options.Value.Storage.Path;
        _folder = Path.IsPathRooted(path) ? path : Path.Combine(environment.ContentRootPath, path);
    }

    public bool Exists(string id) => File.Exists(PathOf(id));

    public StoredAudio? Open(string id)
    {
        var file = new FileInfo(PathOf(id));
        return file.Exists
            ? new StoredAudio(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read), file.LastWriteTimeUtc, file.Length)
            : null;
    }

    public async Task SaveAsync(string id, byte[] audio, CancellationToken cancellationToken)
    {
        // Written aside and moved into place, so a half-written file is never served.
        var path = PathOf(id);
        Directory.CreateDirectory(_folder);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporary, audio, cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    /// <remarks>Ids are 32 hex characters (checked by the caller), so they can never climb out of the folder.</remarks>
    private string PathOf(string id) => Path.Combine(_folder, id + ".mp3");
}

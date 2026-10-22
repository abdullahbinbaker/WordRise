using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Wordrise.Modules.Media.Core.Audio;

namespace Wordrise.Modules.Media.Core.Controllers;

/// <summary>Serves recordings. Public: the landing page plays them too, and they contain no personal data.</summary>
[ApiController]
[Route("api/v1/media/audio")]
[AllowAnonymous]
[Tags("Media")]
internal sealed partial class AudioController(IAudioStore store) : ControllerBase
{
    /// <summary>The recording named by the text's id (see <c>AudioId</c>), or 404 when there is none yet.</summary>
    [HttpGet("{id}")]
    [Produces("audio/mpeg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string id)
    {
        if (!ValidId().IsMatch(id) || store.Open(id) is not { } audio)
        {
            return NotFound();
        }

        // Kept for a day, then checked with the server (ETag): a recording can be redone with another
        // voice, and learners must not keep hearing the old one for months.
        Response.Headers.CacheControl = "public, max-age=86400";

        var lastModified = new DateTimeOffset(audio.LastModifiedUtc, TimeSpan.Zero);
        var etag = new EntityTagHeaderValue($"\"{audio.Length:x}-{audio.LastModifiedUtc.Ticks:x}\"");

        // Range requests are what lets Safari play audio at all.
        return File(audio.Content, "audio/mpeg", lastModified, etag, enableRangeProcessing: true);
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex ValidId();
}

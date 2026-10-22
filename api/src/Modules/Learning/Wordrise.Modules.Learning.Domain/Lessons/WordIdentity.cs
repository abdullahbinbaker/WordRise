using System.Security.Cryptography;
using System.Text;

namespace Wordrise.Modules.Learning.Domain.Lessons;

/// <summary>
/// A word's id comes from its English spelling, so the same word taught in two lessons is
/// the same word to a learner: it is introduced once, and reviewed once.
/// </summary>
public static class WordIdentity
{
    public static Guid For(string english)
    {
        var normalized = string.Join(' ', english.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"wordrise:word:{normalized}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

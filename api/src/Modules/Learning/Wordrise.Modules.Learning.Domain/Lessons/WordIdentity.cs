using System.Security.Cryptography;
using System.Text;

namespace Wordrise.Modules.Learning.Domain.Lessons;

/// <summary>
/// A word's id comes from its language and spelling, so the same word taught in two lessons is
/// the same word to a learner (introduced once, reviewed once), while a word that happens to be
/// spelled the same in two languages stays two words.
/// </summary>
public static class WordIdentity
{
    public static Guid For(string language, string target)
    {
        var normalized = string.Join(' ', target.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"wordrise:word:{language.Trim().ToLowerInvariant()}:{normalized}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

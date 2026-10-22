using System.Text.RegularExpressions;

namespace Wordrise.Modules.Learning.Domain.Lessons;

/// <summary>
/// Languages are named by their ISO code. A lesson teaches one <em>target</em> language (what the
/// learner is studying) and explains it in one <em>native</em> language (the learner's own).
/// </summary>
public static partial class LessonLanguages
{
    public const string English = "en";

    public const string Arabic = "ar";

    public static bool IsValidCode(string? code) => !string.IsNullOrWhiteSpace(code) && Code().IsMatch(code);

    [GeneratedRegex("^[a-z]{2,3}$")]
    private static partial Regex Code();
}

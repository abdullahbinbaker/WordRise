namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <summary>
/// The language pairs lessons can be written for: what is taught, and what it is explained in.
/// Adding a course means adding it here, a curriculum in that language, writer and reviewer
/// instructions for it, and voices for it in the Media settings.
/// </summary>
public static class SupportedCourses
{
    public const string English = "en";

    public const string Arabic = "ar";

    public static bool IsSupported(string targetLanguage, string nativeLanguage) =>
        string.Equals(targetLanguage, English, StringComparison.OrdinalIgnoreCase)
        && string.Equals(nativeLanguage, Arabic, StringComparison.OrdinalIgnoreCase);
}

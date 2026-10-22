namespace Wordrise.Modules.ContentAuthoring.Domain;

/// <summary>Stable error codes clients can rely on.</summary>
public static class AuthoringErrors
{
    public const string BriefInvalid = "authoring.brief_invalid";
    public const string InvalidState = "authoring.invalid_state";
    public const string DraftNotFound = "authoring.draft_not_found";
    public const string FeedbackRequired = "authoring.feedback_required";
    public const string PublishRejected = "authoring.publish_rejected";
}

namespace Wordrise.Modules.Identity.Core.Email;

internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Log writes each message (and its links) to the API log; Smtp sends it through your mail server.</summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Log;

    /// <summary>The sender shown to the learner, for example "Wordrise &lt;no-reply@example.org&gt;".</summary>
    public string From { get; set; } = "";

    public SmtpOptions Smtp { get; set; } = new();
}

internal enum EmailProvider
{
    Log,
    Smtp,
}

internal sealed class SmtpOptions
{
    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string User { get; set; } = "";

    public string Password { get; set; } = "";
}

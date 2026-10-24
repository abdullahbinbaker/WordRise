using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Identity.Core.Domain;

namespace Wordrise.Modules.Identity.Core.Email;

/// <summary>
/// The account emails of the ready-made Identity endpoints, in Arabic. The links open pages of the
/// website (which call the API), not the API itself.
/// </summary>
internal sealed class AccountEmailSender(IMailTransport transport, IOptions<IdentityModuleOptions> options)
    : IEmailSender<AppUser>
{
    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        // The API link carries userId, code (and changedEmail); the website page passes them back to the API.
        // Identity passes the link HTML-encoded (&amp;), so decode it first.
        confirmationLink = HttpUtility.HtmlDecode(confirmationLink);
        var query = HttpUtility.ParseQueryString(new Uri(confirmationLink).Query);
        var link = WebLink(confirmationLink, "confirm-email", ("userId", query["userId"]), ("code", query["code"]), ("changedEmail", query["changedEmail"]));

        return transport.SendAsync(
            email,
            "فعّل حسابك في Wordrise",
            $"أهلاً بك في Wordrise.\n\nاضغط على الرابط لتفعيل بريدك الإلكتروني:\n{link}\n\nإن لم تنشئ حساباً فتجاهل هذه الرسالة.",
            CancellationToken.None);
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        var link = WebLink(null, "reset-password", ("email", email), ("code", resetCode));

        return transport.SendAsync(
            email,
            "إعادة تعيين كلمة المرور في Wordrise",
            $"طلبتَ إعادة تعيين كلمة المرور.\n\nاضغط على الرابط لاختيار كلمة مرور جديدة:\n{link}\n\nإن لم تطلب ذلك فتجاهل هذه الرسالة، وكلمة مرورك لن تتغير.",
            CancellationToken.None);
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) =>
        SendPasswordResetCodeAsync(user, email, HttpUtility.ParseQueryString(new Uri(HttpUtility.HtmlDecode(resetLink)).Query)["code"] ?? "");

    /// <summary>The website address: the configured one, else the address the API link itself used (same site in production).</summary>
    private string WebLink(string? apiLink, string page, params (string Key, string? Value)[] values)
    {
        var baseUrl = options.Value.WebBaseUrl.TrimEnd('/');
        if (baseUrl.Length == 0 && apiLink is not null)
        {
            var uri = new Uri(apiLink);
            baseUrl = uri.GetLeftPart(UriPartial.Authority);
        }

        var query = string.Join("&", values
            .Where(v => !string.IsNullOrEmpty(v.Value))
            .Select(v => $"{v.Key}={Uri.EscapeDataString(v.Value!)}"));
        return $"{baseUrl}/{page}?{query}";
    }
}

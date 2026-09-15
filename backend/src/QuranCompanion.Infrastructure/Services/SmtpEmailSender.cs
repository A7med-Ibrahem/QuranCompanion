using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Infrastructure.Identity;

namespace QuranCompanion.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
private const string InkColor = "#1C2A25";
private const string MutedColor = "#5B685F";
private const string PrimaryColor = "#2F5D50";
private const string AccentColor = "#B08A44";
private const string AccentTint = "#F1E6CE";
private const string PaperColor = "#F4EFE3";
private const string CardColor = "#FBF8F1";
private const string LineColor = "#DFD5BE";


private readonly SmtpSettings _settings;
private readonly ILogger<SmtpEmailSender> _logger;

public SmtpEmailSender(
    IOptions<SmtpSettings> settings,
    ILogger<SmtpEmailSender> logger)
{
    _settings = settings.Value;
    _logger = logger;
}

public Task SendEmailConfirmationAsync(
    string toEmail,
    string displayName,
    string confirmationLink,
    CancellationToken ct = default)
{
    var name = WebUtility.HtmlEncode(displayName);
    var safeConfirmationLink = WebUtility.HtmlEncode(confirmationLink);

    var content = $"""
        <p style='margin:0 0 16px; font-size:16px; color:{InkColor};'>
            أهلًا {name} 👋
        </p>

        <p style='margin:0 0 24px; font-size:15px; line-height:1.8; color:{MutedColor};'>
            يسعدنا انضمامك إلى وِرْد. لتأكيد بريدك الإلكتروني وإتمام إعداد حسابك، اضغط الزر التالي:
        </p>

        <table role='presentation' cellpadding='0' cellspacing='0' style='margin:0 auto 24px;'>
            <tr>
                <td style='background:{PrimaryColor}; border-radius:8px;'>
                    <a href='{safeConfirmationLink}'
                       style='display:inline-block; padding:14px 32px; font-size:15px; font-weight:600; color:#FBF8F1; text-decoration:none;'>
                        تأكيد البريد الإلكتروني
                    </a>
                </td>
            </tr>
        </table>

        <p style='margin:0; font-size:13px; color:{MutedColor};'>
            إذا لم يعمل الزر، انسخ هذا الرابط والصقه في المتصفح:<br/>
            <span style='direction:ltr; display:inline-block; word-break:break-all;'>
                {safeConfirmationLink}
            </span>
        </p>
        """;

    return SendAsync(
        toEmail,
        "تأكيد بريدك الإلكتروني - وِرْد",
        BuildShell("تأكيد البريد الإلكتروني", content),
        ct);
}

public Task SendPasswordResetOtpAsync(
    string toEmail,
    string displayName,
    string code,
    CancellationToken ct = default)
{
    var name = WebUtility.HtmlEncode(displayName);
    var spacedCode = string.Join(" ", code.Select(c => c));

    var content = $"""
        <p style='margin:0 0 16px; font-size:16px; color:{InkColor};'>
            مرحبًا {name}،
        </p>

        <p style='margin:0 0 24px; font-size:15px; line-height:1.8; color:{MutedColor};'>
            وصلنا طلب لإعادة تعيين كلمة المرور الخاصة بحسابك. استخدم الرمز التالي لإتمام العملية:
        </p>

        <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='margin:0 0 24px;'>
            <tr>
                <td align='center'
                    style='background:{AccentTint}; border:1px solid {AccentColor}; border-radius:10px; padding:22px 16px;'>
                    <span style='font-size:34px; font-weight:700; letter-spacing:6px; direction:ltr; color:{AccentColor}; font-family:Courier New, monospace;'>
                        {spacedCode}
                    </span>
                </td>
            </tr>
        </table>

        <p style='margin:0 0 8px; font-size:14px; color:{InkColor};'>
            ⏱️ الرمز صالح لمدة <strong>10 دقائق</strong> فقط.
        </p>

        <p style='margin:0; font-size:13px; color:{MutedColor}; line-height:1.7;'>
            لأمانك: لن نطلب منك مشاركة هذا الرمز مع أي شخص، وفريق وِرْد لن يسألك عنه أبدًا.
            إذا لم تطلب إعادة تعيين كلمة المرور، يمكنك تجاهل هذه الرسالة بأمان.
        </p>
        """;

    return SendAsync(
        toEmail,
        $"{code} — رمز إعادة تعيين كلمة المرور",
        BuildShell("إعادة تعيين كلمة المرور", content),
        ct);
}

private static string BuildShell(string title, string innerContent) => $"""
    <!DOCTYPE html>
    <html dir='rtl' lang='ar'>
    <body style='margin:0; padding:0; background:{PaperColor}; font-family:Tahoma, Arial, sans-serif;'>

        <table role='presentation'
               cellpadding='0'
               cellspacing='0'
               width='100%'
               style='background:{PaperColor}; padding:32px 16px;'>
            <tr>
                <td align='center'>

                    <table role='presentation'
                           cellpadding='0'
                           cellspacing='0'
                           width='100%'
                           style='max-width:480px;'>

                        <tr>
                            <td align='center' style='padding-bottom:24px;'>
                                <div style='font-size:26px; font-weight:700; color:{PrimaryColor};'>
                                    وِرْد
                                </div>

                                <div style='font-size:12px; color:{MutedColor}; margin-top:4px;'>
                                    اقرأ وِردك. تابع رحلتك.
                                </div>
                            </td>
                        </tr>

                        <tr>
                            <td style='background:{CardColor}; border:1px solid {LineColor}; border-radius:16px; padding:32px; text-align:right;'>
                                {innerContent}
                            </td>
                        </tr>

                        <tr>
                            <td align='center' style='padding-top:20px;'>
                                <span style='font-size:11px; color:{MutedColor};'>
                                    {title} · وِرْد
                                </span>
                            </td>
                        </tr>

                    </table>

                </td>
            </tr>
        </table>

    </body>
    </html>
    """;

private async Task SendAsync(
    string toEmail,
    string subject,
    string htmlBody,
    CancellationToken ct = default)
{
    try
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(
                _settings.Username,
                _settings.Password),
            EnableSsl = true
        };

        using var message = new MailMessage
        {
            From = new MailAddress(
                _settings.FromEmail,
                _settings.FromName),

            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,

            SubjectEncoding = System.Text.Encoding.UTF8,
            BodyEncoding = System.Text.Encoding.UTF8
        };

        message.To.Add(toEmail);

        await client.SendMailAsync(message, ct);

        _logger.LogInformation(
            "Sent email to {Email}: {Subject}",
            toEmail,
            subject);
    }
    catch (Exception ex)
    {
        // Log, then propagate - callers (e.g. forgot-password) must know the
        // email did NOT actually go out so they can fail honestly instead of
        // telling the user a code was sent.
        _logger.LogError(
            ex,
            "Failed to send email to {Email}",
            toEmail);
        throw;
    }
}

}

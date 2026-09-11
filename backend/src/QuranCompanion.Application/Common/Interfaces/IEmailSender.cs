namespace QuranCompanion.Application.Common.Interfaces;

/// <summary>
/// Abstraction over outgoing transactional email (verification, password reset).
/// Swap the Infrastructure implementation for SendGrid/SES/SMTP without touching Application logic.
/// </summary>
public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationLink, CancellationToken ct = default);

    /// <summary>Sends a short-lived numeric one-time code (not a link) the user types back in to reset their password.</summary>
    Task SendPasswordResetOtpAsync(string toEmail, string displayName, string code, CancellationToken ct = default);
}

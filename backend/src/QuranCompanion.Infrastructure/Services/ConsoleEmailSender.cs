using Microsoft.Extensions.Logging;
using QuranCompanion.Application.Common.Interfaces;

namespace QuranCompanion.Infrastructure.Services;

/// <summary>
/// Development stand-in for a real email provider. Logs instead of sending.
/// Swap for a SendGrid/SES/SMTP implementation in production by registering
/// a different IEmailSender in DependencyInjection.cs - nothing else changes.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailConfirmationAsync(string toEmail, string displayName, string confirmationLink, CancellationToken ct = default)
    {
        _logger.LogInformation("[DEV EMAIL] Confirmation for {Email} ({Name}): {Link}", toEmail, displayName, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetOtpAsync(string toEmail, string displayName, string code, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "\n========================================\n[DEV EMAIL] Password reset code for {Email} ({Name})\nCODE: {Code}\n(valid 10 minutes)\n========================================",
            toEmail, displayName, code);
        return Task.CompletedTask;
    }
}

using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Auth;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Identity;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _tokenService;
    private readonly IWirdIdGenerator _wirdIdGenerator;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtSettings _jwtSettings;
    private readonly GoogleAuthSettings _googleSettings;

    // Generic messages only - never reveal whether an email exists in the system.
    private const string GenericLoginError = "Incorrect email or password.";
    private const string GenericResetError = "This code is invalid or has expired. Please request a new one.";

    private static readonly System.Text.RegularExpressions.Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IJwtTokenService tokenService,
        IWirdIdGenerator wirdIdGenerator,
        IEmailSender emailSender,
        ILogger<AuthService> logger,
        IOptions<JwtSettings> jwtSettings,
        IOptions<GoogleAuthSettings> googleSettings)
    {
        _userManager = userManager;
        _db = db;
        _tokenService = tokenService;
        _wirdIdGenerator = wirdIdGenerator;
        _emailSender = emailSender;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
        _googleSettings = googleSettings.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            throw new ApiException("An account with this email already exists.", 409, "email_taken");
        }

        var wirdId = await _wirdIdGenerator.GenerateUniqueAsync(ct);

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName,
            WirdId = wirdId,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationApiException(errors);
        }

        // Best-effort email verification; never blocks registration. Failures
        // are logged, never surfaced to the user (the account was already created).
        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmToken));
        var confirmEmail = user.Email!;
        var confirmLink = $"/auth/confirm-email?userId={user.Id}&token={encodedToken}";
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailSender.SendEmailConfirmationAsync(confirmEmail, user.DisplayName, confirmLink, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send email confirmation to {Email}", confirmEmail);
            }
        });

        return await IssueTokensAsync(user, ipAddress: null, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UnauthorizedApiException(GenericLoginError);
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            throw new UnauthorizedApiException(GenericLoginError);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResult> LoginWithGoogleAsync(string googleIdToken, string? ipAddress, CancellationToken ct = default)
    {
        Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(googleIdToken,
                new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleSettings.ClientId }
                });
        }
        catch (Exception)
        {
            // Covers expired/forged/wrong-audience tokens - never leak validation internals to the client.
            throw new UnauthorizedApiException("Google sign-in failed. Please try again.");
        }

        if (!payload.EmailVerified)
        {
            throw new UnauthorizedApiException("This Google account's email isn't verified.");
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user is null)
        {
            var wirdId = await _wirdIdGenerator.GenerateUniqueAsync(ct);
            user = new ApplicationUser
            {
                Email = payload.Email,
                UserName = payload.Email,
                DisplayName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                WirdId = wirdId,
                EmailConfirmed = true, // Google already verified it
                CreatedAtUtc = DateTime.UtcNow
            };

            // No password - this account can only sign in via Google unless the
            // user later sets one from their profile (a future-phase feature).
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                throw new ValidationApiException(errors);
            }
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var hash = _tokenService.Hash(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null || !stored.IsActive)
        {
            throw new UnauthorizedApiException("Your session has expired. Please log in again.");
        }

        // Rotate: revoke the old token and issue a brand new pair.
        stored.RevokedAtUtc = DateTime.UtcNow;

        var result = await IssueTokensAsync(stored.User, ipAddress, ct);

        stored.ReplacedByTokenHash = _tokenService.Hash(result.RefreshToken);
        await _db.SaveChangesAsync(ct);

        return result;
    }

    public async Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        var hash = _tokenService.Hash(refreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);

        if (stored is { IsActive: true })
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email.Trim()))
        {
            throw new ApiException("Please enter a valid email address.", 400, "invalid_email");
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null) return; // never reveal whether the account exists

        // Invalidate any still-usable earlier codes so only the newest one works.
        var previousCodes = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && o.ConsumedAtUtc == null)
            .ToListAsync(ct);
        foreach (var old in previousCodes) old.ConsumedAtUtc = DateTime.UtcNow;

        var code = GenerateNumericOtp(6);
        _db.PasswordResetOtps.Add(new PasswordResetOtp
        {
            UserId = user.Id,
            CodeHash = HashOtp(code),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        });
        await _db.SaveChangesAsync(ct);

        try
        {
            // Await the real delivery status - the user must get a CODE, not a
            // silent success if the SMTP send actually failed.
            await _emailSender.SendPasswordResetOtpAsync(user.Email!, user.DisplayName, code, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset OTP to {Email}", user.Email);
            // Meaningful, generic, transient-looking error. It only fires for an
            // existing account whose verification actually failed to send, which is
            // the honest behaviour the frontend depends on for its own message.
            throw new ApiException(
                "تعذّر إرسال رمز التحقق الآن. حاول مرة أخرى خلال بضع دقائق، أو تواصل مع الدعم.",
                503, "email_send_failed");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        const int maxAttempts = 5;

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex.IsMatch(request.Email.Trim()))
        {
            throw new ApiException("Please enter a valid email address.", 400, "invalid_email");
        }

        if (string.IsNullOrWhiteSpace(request.Code)
            || request.Code.Trim().Length != 6
            || request.Code.Trim().Any(c => c is < '0' or > '9'))
        {
            throw new ApiException(GenericResetError, 400, "invalid_reset_code");
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
        {
            throw new ApiException(GenericResetError, 400, "invalid_reset_code");
        }

        var otp = await _db.PasswordResetOtps
            .Where(o => o.UserId == user.Id && o.ConsumedAtUtc == null)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (otp is null || !otp.IsUsable)
        {
            throw new ApiException(GenericResetError, 400, "invalid_reset_code");
        }

        if (otp.Attempts >= maxAttempts)
        {
            throw new ApiException("Too many attempts. Please request a new code.", 429, "too_many_attempts");
        }

        if (HashOtp(request.Code.Trim()) != otp.CodeHash)
        {
            otp.Attempts++;
            await _db.SaveChangesAsync(ct);
            throw new ApiException("Incorrect code. Please try again.", 400, "incorrect_code");
        }

        otp.ConsumedAtUtc = DateTime.UtcNow;

        // The user might have no password yet (Google-only account) - set one either way.
        IdentityResult result;
        if (await _userManager.HasPasswordAsync(user))
        {
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                throw new ApiException("Something went wrong resetting your password. Please try again.", 500, "reset_failed");
            }
        }
        result = await _userManager.AddPasswordAsync(user, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationApiException(errors);
        }

        // Revoke every active session on password reset.
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var t in activeTokens) t.RevokedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static string GenerateNumericOtp(int digits)
    {
        var max = (int)Math.Pow(10, digits);
        var value = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, max);
        return value.ToString(new string('0', digits));
    }

    private static string HashOtp(string code)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch
        {
            return false;
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        return result.Succeeded;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundApiException("User not found.");
        return ToProfileDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundApiException("User not found.");

        user.DisplayName = request.DisplayName;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new ValidationApiException(errors);
        }

        return ToProfileDto(user);
    }

    private async Task<AuthResult> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.CreateAccessToken(user, roles);
        var accessExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);

        var rawRefreshToken = _tokenService.CreateRefreshTokenValue();
        var refreshExpires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.Hash(rawRefreshToken),
            ExpiresAtUtc = refreshExpires,
            CreatedByIp = ipAddress
        });
        await _db.SaveChangesAsync(ct);

        return new AuthResult(accessToken, accessExpires, rawRefreshToken, refreshExpires, ToProfileDto(user));
    }

    private static UserProfileDto ToProfileDto(ApplicationUser user) => new(
        user.Id, user.Email!, user.DisplayName, user.WirdId, user.EmailConfirmed, user.CreatedAtUtc);
}
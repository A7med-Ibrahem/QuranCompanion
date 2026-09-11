namespace QuranCompanion.Application.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record GoogleLoginRequest(string IdToken);

public record LoginRequest(string Email, string Password);

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public record UpdateProfileRequest(string DisplayName);

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    string WirdId,
    bool EmailConfirmed,
    DateTime CreatedAtUtc
);

/// <summary>
/// Result of any auth operation that issues tokens (register/login/refresh).
/// AccessToken is short-lived (JWT); RefreshToken is long-lived and opaque.
/// </summary>
public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserProfileDto User
);

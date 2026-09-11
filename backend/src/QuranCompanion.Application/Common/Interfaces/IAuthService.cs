using QuranCompanion.Application.DTOs.Auth;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResult> LoginWithGoogleAsync(string googleIdToken, string? ipAddress, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default);
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
}

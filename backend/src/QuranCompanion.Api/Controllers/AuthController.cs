using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Auth;

namespace QuranCompanion.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private const string RefreshCookieName = "qc_refresh";

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return UnprocessableEntity(ApiResponse<object>.Fail("Email, password and display name are required.", "validation_error"));
        }

        var result = await _authService.RegisterAsync(request, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(ApiResponse<object>.Ok(ToAuthResponse(result)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return UnprocessableEntity(ApiResponse<object>.Fail("Please enter your email and password.", "validation_error"));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(ApiResponse<object>.Ok(ToAuthResponse(result)));
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginWithGoogleAsync(request.IdToken, ip, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(ApiResponse<object>.Ok(ToAuthResponse(result)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new UnauthorizedApiException("Your session has expired. Please log in again.");
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(refreshToken, ip, ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAtUtc);
        return Ok(ApiResponse<object>.Ok(ToAuthResponse(result)));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        var userId = CurrentUserId();
        if (!string.IsNullOrEmpty(refreshToken) && userId.HasValue)
        {
            await _authService.LogoutAsync(userId.Value, refreshToken, ct);
        }
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = "/api/auth",
            Secure = true,
            SameSite = SameSiteMode.None
        });
        return Ok(ApiResponse<object>.Ok(new { message = "Logged out successfully." }));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordBody body, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(body.Email, ct);
        // Always return success - never reveal whether the email exists.
        return Ok(ApiResponse<object>.Ok(new { message = "If an account exists for that email, a reset link has been sent." }));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Your password has been reset. Please log in." }));
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token, CancellationToken ct)
    {
        var confirmed = await _authService.ConfirmEmailAsync(userId, token, ct);
        if (!confirmed)
        {
            return BadRequest(ApiResponse<object>.Fail("This confirmation link is invalid or has expired.", "invalid_confirmation_token"));
        }
        return Ok(ApiResponse<object>.Ok(new { message = "Email confirmed." }));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = CurrentUserId()!.Value;
        var profile = await _authService.GetProfileAsync(userId, ct);
        return Ok(ApiResponse<object>.Ok(profile));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId()!.Value;
        var profile = await _authService.UpdateProfileAsync(userId, request, ct);
        return Ok(ApiResponse<object>.Ok(profile));
    }

    private Guid? CurrentUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private void SetRefreshCookie(string token, DateTime expiresUtc)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            // Frontend (Vercel) and API (MonsterASP) live on different domains,
            // which makes every request cross-site from the browser's point of
            // view. Cross-site cookies are ONLY sent when SameSite=None, and
            // SameSite=None is only honored by browsers when Secure=true - so
            // both must be hardcoded rather than derived from Request.IsHttps.
            // This does mean the cookie no longer works over plain http, but
            // both hosts serve HTTPS in production, and local dev can just use
            // https://localhost as well (or relax this back to Lax/IsHttps when
            // running everything on the same origin).
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresUtc,
            Path = "/api/auth"
        });
    }

    // Refresh token never leaves the server in the JSON body - only via httpOnly cookie.
    private static object ToAuthResponse(AuthResult result) => new
    {
        accessToken = result.AccessToken,
        accessTokenExpiresAtUtc = result.AccessTokenExpiresAtUtc,
        user = result.User
    };
}

public record ForgotPasswordBody(string Email);

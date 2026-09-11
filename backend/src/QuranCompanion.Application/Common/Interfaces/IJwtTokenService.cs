using System.Security.Claims;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Creates a short-lived signed JWT access token for the user.</summary>
    string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>Creates a cryptographically random opaque refresh token (raw value, not hashed).</summary>
    string CreateRefreshTokenValue();

    string Hash(string rawToken);

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
}

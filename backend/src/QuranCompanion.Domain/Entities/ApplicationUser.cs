using Microsoft.AspNetCore.Identity;

namespace QuranCompanion.Domain.Entities;

/// <summary>
/// The application user. Extends ASP.NET Identity's user with the
/// public-facing, shareable Wird ID and profile fields.
/// Email is never exposed to other users - only WirdId is shareable.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Public, shareable identifier used to connect with companions.
    /// Format: WIRD-XXXXXXX (uppercase alphanumeric). Never the email.
    /// </summary>
    public string WirdId { get; set; } = default!;

    public string DisplayName { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAtUtc { get; set; }

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

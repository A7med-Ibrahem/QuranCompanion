namespace QuranCompanion.Domain.Entities;

/// <summary>
/// Long-lived opaque token used to mint new access tokens without
/// forcing the user to re-authenticate with a password.
/// One row per issued token so any single device/session can be revoked.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    /// <summary>Stored as a SHA-256 hash, never the raw token.</summary>
    public string TokenHash { get; set; } = default!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
}

namespace QuranCompanion.Domain.Entities;

/// <summary>
/// A short-lived numeric one-time code for password reset (replaces the
/// older link-with-token flow). Only the SHA-256 hash is stored - the raw
/// code exists only in the email sent to the user and briefly in memory.
/// </summary>
public class PasswordResetOtp
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public string CodeHash { get; set; } = default!;

    public int Attempts { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public bool IsUsable => ConsumedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}

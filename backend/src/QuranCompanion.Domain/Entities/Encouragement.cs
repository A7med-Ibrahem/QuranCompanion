namespace QuranCompanion.Domain.Entities;

/// <summary>
/// A short, predefined encouragement message sent between accepted companions
/// (spec section 7). Free-text is deliberately not supported - message must
/// be one of a fixed whitelist (see EncouragementMessages) to keep this from
/// turning into a social-media-style DM feature.
/// </summary>
public class Encouragement
{
    public int Id { get; set; }

    public Guid FromUserId { get; set; }
    public ApplicationUser FromUser { get; set; } = default!;

    public Guid ToUserId { get; set; }
    public ApplicationUser ToUser { get; set; } = default!;

    public string Message { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

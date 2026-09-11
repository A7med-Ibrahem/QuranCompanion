namespace QuranCompanion.Domain.Entities;

/// <summary>
/// One row per user - their single "continue reading" position (spec section 4).
/// Overwritten in place as they read; history/streaks are separate concerns
/// added in later phases, not derived from this table.
/// </summary>
public class ReadingProgress
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

namespace QuranCompanion.Domain.Entities;

/// <summary>
/// A user's private saved ayah, with an optional personal note (spec section 11).
/// Bookmarks and notes are never visible to anyone but their owner.
/// </summary>
public class Bookmark
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

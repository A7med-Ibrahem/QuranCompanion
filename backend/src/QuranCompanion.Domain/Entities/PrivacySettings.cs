namespace QuranCompanion.Domain.Entities;

/// <summary>
/// Controls what a user's *accepted companions* may see about their Wird
/// activity (spec section 6). Never controls anything about non-companions -
/// there are no public profiles. Default prioritizes privacy: only a plain
/// completion checkmark is shared; everything else is opt-in.
/// </summary>
public class PrivacySettings
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public bool ShareCompletionStatus { get; set; } = true;
    public bool ShareStreak { get; set; } = false;
    public bool ShareWirdRange { get; set; } = false;       // exact pages/ayahs read today
    public bool ShareReadingProgress { get; set; } = false; // last surah/ayah position

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

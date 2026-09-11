namespace QuranCompanion.Domain.Entities;

/// <summary>
/// One row per user per calendar day they completed their Wird. The unique
/// (UserId, CompletionDate) index is what makes "prevent accidental duplicate
/// completion" (spec section 5) a database guarantee, not just a UI check.
/// Storing the exact global-ayah range read (rather than recomputing it later)
/// keeps history accurate even if the user's Wird plan changes afterward.
/// </summary>
public class WirdCompletion
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public DateOnly CompletionDate { get; set; }

    public int StartGlobalAyah { get; set; }
    public int EndGlobalAyah { get; set; }

    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}

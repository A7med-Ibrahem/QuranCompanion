namespace QuranCompanion.Domain.Entities;

public enum WirdType
{
    OnePage = 0,
    FivePages = 1,
    TenPages = 2,
    QuarterJuz = 3,
    HalfJuz = 4,
    OneJuz = 5,
    Custom = 6,

    /// <summary>Daily ayah count is derived, not fixed - see TargetCompletionDate (spec section 14).</summary>
    GoalBased = 7
}

/// <summary>
/// The user's current daily Wird setting (spec section 5). One row per user -
/// changing it takes effect starting with the next day's calculation; it
/// never rewrites a day that's already been completed.
/// </summary>
public class WirdPlan
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public WirdType Type { get; set; }

    /// <summary>Only used when Type == Custom - number of ayahs per day.</summary>
    public int? CustomAyahsPerDay { get; set; }

    /// <summary>
    /// Only used when Type == GoalBased - "finish the Quran by this date".
    /// Today's ayah count is recalculated from the *remaining* ayahs and
    /// remaining days every time it's asked for, so falling behind
    /// automatically raises tomorrow's portion instead of silently missing
    /// the goal (spec section 16's "adjust if you fall behind", applied
    /// generally rather than just in Ramadan mode).
    /// </summary>
    public DateOnly? TargetCompletionDate { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

namespace QuranCompanion.Domain.Entities;

/// <summary>
/// A group's collaborative target - "finish the Quran together by this date"
/// (spec section 15). One active goal per group; setting a new one replaces
/// the old target rather than layering multiple goals. Progress is tracked
/// per member from their own overall reading history, never ranked against
/// each other - the group view only ever shows a checkmark/percent per
/// person, never a leaderboard.
/// </summary>
public class SharedGoal
{
    public int GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public DateOnly TargetCompletionDate { get; set; }

    public Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

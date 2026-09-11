namespace QuranCompanion.Domain.Entities;

/// <summary>
/// A small private circle of companions (spec section 15 - "two or more
/// connected companions"). Membership is only ever grown by an existing
/// member vouching for one of *their own* accepted companions - there's no
/// public discovery or open invites, keeping the whole thing as trusted and
/// private as a 1-to-1 companion connection.
/// </summary>
public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public Guid CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}

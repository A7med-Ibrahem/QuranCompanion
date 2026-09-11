namespace QuranCompanion.Domain.Entities;

public class GroupMember
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = default!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}

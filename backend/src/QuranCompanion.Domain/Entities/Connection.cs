namespace QuranCompanion.Domain.Entities;

public enum ConnectionStatus
{
    Pending = 0,
    Accepted = 1
}

/// <summary>
/// A private link between two users (spec section 2). UserAId is always the
/// lexicographically smaller Guid of the pair - purely so the unique index
/// below can prevent duplicate connections regardless of who requested it or
/// in what order. RequestedByUserId is the only one who can't Accept/Reject
/// their own outgoing request.
/// </summary>
public class Connection
{
    public int Id { get; set; }

    public Guid UserAId { get; set; }
    public ApplicationUser UserA { get; set; } = default!;

    public Guid UserBId { get; set; }
    public ApplicationUser UserB { get; set; } = default!;

    public Guid RequestedByUserId { get; set; }

    public ConnectionStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }
}

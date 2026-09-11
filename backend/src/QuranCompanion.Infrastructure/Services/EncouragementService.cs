using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Companions;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class EncouragementService : IEncouragementService
{
    // Deliberately a closed set (spec section 7) - keeps this from becoming a
    // general messaging feature and avoids needing to moderate free text.
    public IReadOnlyList<string> AllowedMessages { get; } = new[]
    {
        "استمر 🤍",
        "بارك الله في رحلتك.",
        "أحسنت على استمرارك.",
        "تقبل الله منك."
    };

    private readonly AppDbContext _db;

    public EncouragementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EncouragementDto> SendAsync(Guid fromUserId, Guid toUserId, string message, CancellationToken ct = default)
    {
        if (!AllowedMessages.Contains(message))
        {
            throw new ApiException("Please choose one of the suggested messages.", 400, "invalid_encouragement_message");
        }

        if (fromUserId == toUserId)
        {
            throw new ApiException("You can't send encouragement to yourself.", 400, "cannot_encourage_self");
        }

        var (a, b) = fromUserId.CompareTo(toUserId) < 0 ? (fromUserId, toUserId) : (toUserId, fromUserId);
        var isConnected = await _db.Connections.AnyAsync(
            c => c.UserAId == a && c.UserBId == b && c.Status == ConnectionStatus.Accepted, ct);
        if (!isConnected)
        {
            throw new ApiException("You can only encourage a connected companion.", 403, "not_companions");
        }

        var encouragement = new Encouragement { FromUserId = fromUserId, ToUserId = toUserId, Message = message };
        _db.Encouragements.Add(encouragement);
        await _db.SaveChangesAsync(ct);

        var fromUser = await _db.Users.FirstAsync(u => u.Id == fromUserId, ct);
        return new EncouragementDto(encouragement.Id, fromUserId, fromUser.DisplayName, message, encouragement.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<EncouragementDto>> GetReceivedAsync(Guid userId, CancellationToken ct = default)
    {
        var received = await _db.Encouragements
            .Include(e => e.FromUser)
            .Where(e => e.ToUserId == userId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        return received
            .Select(e => new EncouragementDto(e.Id, e.FromUserId, e.FromUser.DisplayName, e.Message, e.CreatedAtUtc))
            .ToList();
    }
}

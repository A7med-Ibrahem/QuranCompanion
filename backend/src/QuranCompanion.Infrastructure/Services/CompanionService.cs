using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Companions;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class CompanionService : ICompanionService
{
    private readonly AppDbContext _db;
    private readonly IWirdService _wirdService;
    private readonly IReadingProgressService _readingProgressService;
    private readonly IPrivacyService _privacyService;

    public CompanionService(
        AppDbContext db,
        IWirdService wirdService,
        IReadingProgressService readingProgressService,
        IPrivacyService privacyService)
    {
        _db = db;
        _wirdService = wirdService;
        _readingProgressService = readingProgressService;
        _privacyService = privacyService;
    }

    public async Task<ConnectionRequestDto> SendRequestAsync(Guid userId, string wirdId, CancellationToken ct = default)
    {
        var normalizedWirdId = wirdId.Trim().ToUpperInvariant();

        var target = await _db.Users.FirstOrDefaultAsync(u => u.WirdId == normalizedWirdId, ct)
            ?? throw new NotFoundApiException("No user was found with this Wird ID.");

        if (target.Id == userId)
        {
            throw new ApiException("You cannot connect with yourself.", 400, "cannot_connect_self");
        }

        var (a, b) = OrderPair(userId, target.Id);
        var existing = await _db.Connections.FirstOrDefaultAsync(c => c.UserAId == a && c.UserBId == b, ct);

        if (existing is not null)
        {
            if (existing.Status == ConnectionStatus.Accepted)
            {
                throw new ApiException("You are already connected with this companion.", 409, "already_connected");
            }
            throw new ApiException("This connection request already exists.", 409, "request_exists");
        }

        var connection = new Connection
        {
            UserAId = a,
            UserBId = b,
            RequestedByUserId = userId,
            Status = ConnectionStatus.Pending
        };
        _db.Connections.Add(connection);
        await _db.SaveChangesAsync(ct);

        return new ConnectionRequestDto(connection.Id, target.Id, target.DisplayName, target.WirdId, connection.CreatedAtUtc);
    }

    public async Task<CompanionDto> AcceptAsync(Guid userId, int connectionId, CancellationToken ct = default)
    {
        var connection = await LoadOwnedConnectionAsync(userId, connectionId, ct);

        if (connection.Status != ConnectionStatus.Pending)
        {
            throw new ApiException("This request is no longer pending.", 400, "not_pending");
        }
        if (connection.RequestedByUserId == userId)
        {
            throw new ApiException("You can't accept your own request.", 400, "cannot_accept_own_request");
        }

        connection.Status = ConnectionStatus.Accepted;
        connection.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var other = await OtherUserAsync(connection, userId, ct);
        return new CompanionDto(connection.Id, other.Id, other.DisplayName, other.WirdId, connection.RespondedAtUtc!.Value);
    }

    public async Task RejectAsync(Guid userId, int connectionId, CancellationToken ct = default)
    {
        var connection = await LoadOwnedConnectionAsync(userId, connectionId, ct);

        if (connection.Status != ConnectionStatus.Pending)
        {
            throw new ApiException("This request is no longer pending.", 400, "not_pending");
        }
        if (connection.RequestedByUserId == userId)
        {
            throw new ApiException("You can't reject your own request.", 400, "cannot_reject_own_request");
        }

        _db.Connections.Remove(connection);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid userId, int connectionId, CancellationToken ct = default)
    {
        var connection = await LoadOwnedConnectionAsync(userId, connectionId, ct);
        _db.Connections.Remove(connection);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CompanionDto>> GetCompanionsAsync(Guid userId, CancellationToken ct = default)
    {
        var connections = await _db.Connections
            .Include(c => c.UserA)
            .Include(c => c.UserB)
            .Where(c => (c.UserAId == userId || c.UserBId == userId) && c.Status == ConnectionStatus.Accepted)
            .OrderByDescending(c => c.RespondedAtUtc)
            .ToListAsync(ct);

        return connections.Select(c =>
        {
            var other = c.UserAId == userId ? c.UserB : c.UserA;
            return new CompanionDto(c.Id, other.Id, other.DisplayName, other.WirdId, c.RespondedAtUtc ?? c.CreatedAtUtc);
        }).ToList();
    }

    public async Task<IReadOnlyList<ConnectionRequestDto>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var connections = await _db.Connections
            .Include(c => c.UserA)
            .Include(c => c.UserB)
            .Where(c => (c.UserAId == userId || c.UserBId == userId)
                        && c.Status == ConnectionStatus.Pending
                        && c.RequestedByUserId != userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        return connections.Select(c =>
        {
            var requester = c.UserAId == c.RequestedByUserId ? c.UserA : c.UserB;
            return new ConnectionRequestDto(c.Id, requester.Id, requester.DisplayName, requester.WirdId, c.CreatedAtUtc);
        }).ToList();
    }

    public async Task<IReadOnlyList<ConnectionRequestDto>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var connections = await _db.Connections
            .Include(c => c.UserA)
            .Include(c => c.UserB)
            .Where(c => c.RequestedByUserId == userId && c.Status == ConnectionStatus.Pending)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        return connections.Select(c =>
        {
            var recipient = c.UserAId == userId ? c.UserB : c.UserA;
            return new ConnectionRequestDto(c.Id, recipient.Id, recipient.DisplayName, recipient.WirdId, c.CreatedAtUtc);
        }).ToList();
    }

    public async Task<CompanionStatusDto> GetCompanionStatusAsync(Guid viewerId, Guid companionUserId, CancellationToken ct = default)
    {
        var (a, b) = viewerId.CompareTo(companionUserId) < 0 ? (viewerId, companionUserId) : (companionUserId, viewerId);
        var connected = await _db.Connections.AnyAsync(
            c => c.UserAId == a && c.UserBId == b && c.Status == ConnectionStatus.Accepted, ct);
        if (!connected)
        {
            throw new NotFoundApiException("This user isn't one of your companions.");
        }

        var companion = await _db.Users.FirstOrDefaultAsync(u => u.Id == companionUserId, ct)
            ?? throw new NotFoundApiException("User not found.");

        var privacy = await _privacyService.GetAsync(companionUserId, ct);

        bool? isCompletedToday = null;
        string? todayRangeSummary = null;
        if (privacy.ShareCompletionStatus || privacy.ShareWirdRange)
        {
            var today = await _wirdService.GetTodayAsync(companionUserId, ct);
            if (today is not null)
            {
                if (privacy.ShareCompletionStatus) isCompletedToday = today.IsCompletedToday;
                if (privacy.ShareWirdRange)
                {
                    todayRangeSummary = today.StartSurah == today.EndSurah
                        ? $"سورة {today.StartSurahName} — آية {today.StartAyah} إلى {today.EndAyah}"
                        : $"من سورة {today.StartSurahName} آية {today.StartAyah} إلى سورة {today.EndSurahName} آية {today.EndAyah}";
                }
            }
        }

        int? currentStreak = null;
        int? sharedStreak = null;
        if (privacy.ShareStreak)
        {
            currentStreak = await _wirdService.GetCurrentStreakAsync(companionUserId, ct);
            sharedStreak = await _wirdService.GetSharedStreakAsync(viewerId, companionUserId, ct);
        }

        string? lastReadPositionSummary = null;
        if (privacy.ShareReadingProgress)
        {
            var progress = await _readingProgressService.GetAsync(companionUserId, ct);
            if (progress is not null)
            {
                lastReadPositionSummary = $"سورة {progress.SurahArabicName} — آية {progress.AyahNumber}";
            }
        }

        return new CompanionStatusDto(
            companionUserId, companion.DisplayName, isCompletedToday, currentStreak, sharedStreak,
            todayRangeSummary, lastReadPositionSummary);
    }

    private async Task<Connection> LoadOwnedConnectionAsync(Guid userId, int connectionId, CancellationToken ct)
    {
        var connection = await _db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId, ct)
            ?? throw new NotFoundApiException("Connection not found.");

        if (connection.UserAId != userId && connection.UserBId != userId)
        {
            throw new NotFoundApiException("Connection not found.");
        }

        return connection;
    }

    private async Task<Domain.Entities.ApplicationUser> OtherUserAsync(Connection connection, Guid userId, CancellationToken ct)
    {
        var otherId = connection.UserAId == userId ? connection.UserBId : connection.UserAId;
        return await _db.Users.FirstAsync(u => u.Id == otherId, ct);
    }

    private static (Guid a, Guid b) OrderPair(Guid x, Guid y) =>
        x.CompareTo(y) < 0 ? (x, y) : (y, x);
}

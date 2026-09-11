namespace QuranCompanion.Application.DTOs.Companions;

public record SendConnectionRequestRequest(string WirdId);

public record CompanionDto(
    int ConnectionId,
    Guid UserId,
    string DisplayName,
    string WirdId,
    DateTime ConnectedAtUtc
);

public record ConnectionRequestDto(
    int ConnectionId,
    Guid UserId,
    string DisplayName,
    string WirdId,
    DateTime RequestedAtUtc
);

/// <summary>
/// What the viewer is permitted to see about a companion's activity today,
/// already filtered according to that companion's own privacy settings
/// (spec section 6) - fields the companion hasn't opted to share are null.
/// </summary>
public record CompanionStatusDto(
    Guid CompanionUserId,
    string DisplayName,
    bool? IsCompletedToday,
    int? CurrentStreak,
    int? SharedStreak,
    string? TodayRangeSummary,       // e.g. "من سورة البقرة آية 1 إلى آية 10"
    string? LastReadPositionSummary  // e.g. "سورة آل عمران — آية 42"
);

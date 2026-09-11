namespace QuranCompanion.Application.DTOs.Groups;

public record CreateGroupRequest(string Name);
public record AddGroupMemberRequest(string WirdId);
public record SetSharedGoalRequest(DateOnly TargetCompletionDate);

public record GroupSummaryDto(int Id, string Name, int MemberCount, DateTime CreatedAtUtc);

public record SharedGoalDto(DateOnly TargetCompletionDate, int DaysRemaining, Guid CreatedByUserId);

public record GroupMemberStatusDto(
    Guid UserId,
    string DisplayName,
    string WirdId,
    bool IsCreator,
    bool? IsCompletedToday,
    int? CurrentStreak,

    /// <summary>% of the whole Quran this member has ever completed via their own Wird (0-100). Never a ranking - just their own progress.</summary>
    int? OverallProgressPercent
);

public record GroupDetailDto(
    int Id,
    string Name,
    Guid CreatedByUserId,
    SharedGoalDto? SharedGoal,
    IReadOnlyList<GroupMemberStatusDto> Members
);

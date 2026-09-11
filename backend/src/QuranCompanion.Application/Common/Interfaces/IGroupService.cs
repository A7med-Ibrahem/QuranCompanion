using QuranCompanion.Application.DTOs.Groups;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IGroupService
{
    Task<GroupSummaryDto> CreateAsync(Guid userId, string name, CancellationToken ct = default);
    Task<IReadOnlyList<GroupSummaryDto>> GetMyGroupsAsync(Guid userId, CancellationToken ct = default);
    Task<GroupDetailDto> GetDetailAsync(Guid userId, int groupId, CancellationToken ct = default);

    /// <summary>The member being added must be an accepted companion of the requesting member.</summary>
    Task<GroupDetailDto> AddMemberAsync(Guid requestingUserId, int groupId, string wirdId, CancellationToken ct = default);

    /// <summary>A member may remove themselves; the creator may remove anyone.</summary>
    Task RemoveMemberAsync(Guid requestingUserId, int groupId, Guid memberUserId, CancellationToken ct = default);

    Task DeleteAsync(Guid userId, int groupId, CancellationToken ct = default);

    /// <summary>Sets or replaces the group's shared "finish together by this date" goal (spec section 15). Creator only.</summary>
    Task<GroupDetailDto> SetSharedGoalAsync(Guid userId, int groupId, DateOnly targetCompletionDate, CancellationToken ct = default);
}

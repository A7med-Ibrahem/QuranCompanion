using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Groups;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class GroupService : IGroupService
{
    private const int TotalAyahs = 6236;

    private readonly AppDbContext _db;
    private readonly IWirdService _wirdService;
    private readonly IPrivacyService _privacyService;

    public GroupService(AppDbContext db, IWirdService wirdService, IPrivacyService privacyService)
    {
        _db = db;
        _wirdService = wirdService;
        _privacyService = privacyService;
    }

    public async Task<GroupSummaryDto> CreateAsync(Guid userId, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ApiException("Please enter a group name.", 400, "validation_error");
        }

        var group = new Group { Name = name.Trim(), CreatedByUserId = userId };
        group.Members.Add(new GroupMember { UserId = userId });

        _db.Groups.Add(group);
        await _db.SaveChangesAsync(ct);

        return new GroupSummaryDto(group.Id, group.Name, 1, group.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<GroupSummaryDto>> GetMyGroupsAsync(Guid userId, CancellationToken ct = default)
    {
        var groups = await _db.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.Group)
            .Select(g => new GroupSummaryDto(g.Id, g.Name, g.Members.Count, g.CreatedAtUtc))
            .ToListAsync(ct);

        return groups;
    }

    public async Task<GroupDetailDto> GetDetailAsync(Guid userId, int groupId, CancellationToken ct = default)
    {
        var group = await LoadGroupWithMembersAsync(groupId, ct);
        EnsureIsMember(group, userId);

        var goal = await _db.SharedGoals.FirstOrDefaultAsync(g => g.GroupId == groupId, ct);
        var goalDto = goal is null ? null : ToGoalDto(goal);

        var members = new List<GroupMemberStatusDto>(group.Members.Count);
        foreach (var member in group.Members)
        {
            members.Add(await BuildMemberStatusAsync(member, viewerId: userId, group.CreatedByUserId, ct));
        }

        return new GroupDetailDto(group.Id, group.Name, group.CreatedByUserId, goalDto, members);
    }

    public async Task<GroupDetailDto> AddMemberAsync(Guid requestingUserId, int groupId, string wirdId, CancellationToken ct = default)
    {
        var group = await LoadGroupWithMembersAsync(groupId, ct);
        EnsureIsMember(group, requestingUserId);

        var normalizedWirdId = wirdId.Trim().ToUpperInvariant();
        var target = await _db.Users.FirstOrDefaultAsync(u => u.WirdId == normalizedWirdId, ct)
            ?? throw new NotFoundApiException("No user was found with this Wird ID.");

        if (group.Members.Any(m => m.UserId == target.Id))
        {
            throw new ApiException("This person is already in the group.", 409, "already_group_member");
        }

        // Trust boundary: you can only bring in people *you're* already privately
        // connected with - a group can never be used to reach someone you don't
        // already know, keeping it as private as a direct companion link.
        var (a, b) = requestingUserId.CompareTo(target.Id) < 0 ? (requestingUserId, target.Id) : (target.Id, requestingUserId);
        var isCompanion = await _db.Connections.AnyAsync(
            c => c.UserAId == a && c.UserBId == b && c.Status == ConnectionStatus.Accepted, ct);
        if (!isCompanion)
        {
            throw new ApiException("You can only add your own companions to a group.", 400, "not_your_companion");
        }

        _db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = target.Id });
        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(requestingUserId, groupId, ct);
    }

    public async Task RemoveMemberAsync(Guid requestingUserId, int groupId, Guid memberUserId, CancellationToken ct = default)
    {
        var group = await LoadGroupWithMembersAsync(groupId, ct);
        EnsureIsMember(group, requestingUserId);

        if (requestingUserId != memberUserId && requestingUserId != group.CreatedByUserId)
        {
            throw new ApiException("Only the group creator can remove other members.", 403, "not_group_creator");
        }

        var membership = group.Members.FirstOrDefault(m => m.UserId == memberUserId)
            ?? throw new NotFoundApiException("This person isn't in the group.");

        _db.GroupMembers.Remove(membership);

        // If that was the last member, there's nothing left to keep around.
        if (group.Members.Count <= 1)
        {
            _db.Groups.Remove(group);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, int groupId, CancellationToken ct = default)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct)
            ?? throw new NotFoundApiException("Group not found.");

        if (group.CreatedByUserId != userId)
        {
            throw new ApiException("Only the group creator can delete this group.", 403, "not_group_creator");
        }

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<GroupDetailDto> SetSharedGoalAsync(Guid userId, int groupId, DateOnly targetCompletionDate, CancellationToken ct = default)
    {
        var group = await LoadGroupWithMembersAsync(groupId, ct);
        EnsureIsMember(group, userId);

        if (group.CreatedByUserId != userId)
        {
            throw new ApiException("Only the group creator can set the shared goal.", 403, "not_group_creator");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (targetCompletionDate <= today)
        {
            throw new ApiException("Please choose a target date in the future.", 400, "invalid_target_date");
        }

        var goal = await _db.SharedGoals.FirstOrDefaultAsync(g => g.GroupId == groupId, ct);
        if (goal is null)
        {
            goal = new SharedGoal { GroupId = groupId, CreatedByUserId = userId };
            _db.SharedGoals.Add(goal);
        }
        goal.TargetCompletionDate = targetCompletionDate;

        await _db.SaveChangesAsync(ct);

        return await GetDetailAsync(userId, groupId, ct);
    }

    private async Task<Group> LoadGroupWithMembersAsync(int groupId, CancellationToken ct)
    {
        return await _db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct)
            ?? throw new NotFoundApiException("Group not found.");
    }

    private static void EnsureIsMember(Group group, Guid userId)
    {
        if (group.Members.All(m => m.UserId != userId))
        {
            throw new NotFoundApiException("Group not found.");
        }
    }

    private static SharedGoalDto ToGoalDto(SharedGoal goal)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysRemaining = Math.Max(0, goal.TargetCompletionDate.DayNumber - today.DayNumber);
        return new SharedGoalDto(goal.TargetCompletionDate, daysRemaining, goal.CreatedByUserId);
    }

    private async Task<GroupMemberStatusDto> BuildMemberStatusAsync(GroupMember member, Guid viewerId, Guid createdByUserId, CancellationToken ct)
    {
        var isSelf = member.UserId == viewerId;
        var privacy = await _privacyService.GetAsync(member.UserId, ct);

        bool? isCompletedToday = null;
        if (isSelf || privacy.ShareCompletionStatus)
        {
            var today = await _wirdService.GetTodayAsync(member.UserId, ct);
            isCompletedToday = today?.IsCompletedToday;
        }

        int? currentStreak = null;
        if (isSelf || privacy.ShareStreak)
        {
            currentStreak = await _wirdService.GetCurrentStreakAsync(member.UserId, ct);
        }

        int? progressPercent = null;
        if (isSelf || privacy.ShareStreak)
        {
            progressPercent = await GetOverallProgressPercentAsync(member.UserId, ct);
        }

        return new GroupMemberStatusDto(
            member.UserId, member.User.DisplayName, member.User.WirdId,
            member.UserId == createdByUserId, isCompletedToday, currentStreak, progressPercent);
    }

    /// <summary>% of the whole Quran this person has ever covered via completed Wirds - a personal progress figure, never compared to anyone else's.</summary>
    private async Task<int> GetOverallProgressPercentAsync(Guid userId, CancellationToken ct)
    {
        var totalAyahsCompleted = await _db.WirdCompletions
            .Where(c => c.UserId == userId)
            .SumAsync(c => (int?)(c.EndGlobalAyah - c.StartGlobalAyah + 1), ct) ?? 0;

        return Math.Min(100, (int)Math.Round(totalAyahsCompleted * 100.0 / TotalAyahs));
    }
}

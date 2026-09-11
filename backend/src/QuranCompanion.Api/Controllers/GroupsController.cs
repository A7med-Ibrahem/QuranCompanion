using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Groups;

namespace QuranCompanion.Api.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups(CancellationToken ct)
    {
        var groups = await _groupService.GetMyGroupsAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(groups));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGroupRequest request, CancellationToken ct)
    {
        var group = await _groupService.CreateAsync(CurrentUserId(), request.Name, ct);
        return Ok(ApiResponse<object>.Ok(group));
    }

    [HttpGet("{groupId:int}")]
    public async Task<IActionResult> GetDetail(int groupId, CancellationToken ct)
    {
        var detail = await _groupService.GetDetailAsync(CurrentUserId(), groupId, ct);
        return Ok(ApiResponse<object>.Ok(detail));
    }

    [HttpPost("{groupId:int}/members")]
    public async Task<IActionResult> AddMember(int groupId, AddGroupMemberRequest request, CancellationToken ct)
    {
        var detail = await _groupService.AddMemberAsync(CurrentUserId(), groupId, request.WirdId, ct);
        return Ok(ApiResponse<object>.Ok(detail));
    }

    [HttpDelete("{groupId:int}/members/{memberUserId:guid}")]
    public async Task<IActionResult> RemoveMember(int groupId, Guid memberUserId, CancellationToken ct)
    {
        await _groupService.RemoveMemberAsync(CurrentUserId(), groupId, memberUserId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Member removed." }));
    }

    [HttpDelete("{groupId:int}")]
    public async Task<IActionResult> Delete(int groupId, CancellationToken ct)
    {
        await _groupService.DeleteAsync(CurrentUserId(), groupId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Group deleted." }));
    }

    [HttpPut("{groupId:int}/goal")]
    public async Task<IActionResult> SetGoal(int groupId, SetSharedGoalRequest request, CancellationToken ct)
    {
        var detail = await _groupService.SetSharedGoalAsync(CurrentUserId(), groupId, request.TargetCompletionDate, ct);
        return Ok(ApiResponse<object>.Ok(detail));
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

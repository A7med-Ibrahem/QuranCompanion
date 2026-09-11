using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Api.Controllers;

[ApiController]
[Route("api/wird")]
[Authorize]
public class WirdController : ControllerBase
{
    private readonly IWirdService _wirdService;

    public WirdController(IWirdService wirdService)
    {
        _wirdService = wirdService;
    }

    [HttpGet("plan")]
    public async Task<IActionResult> GetPlan(CancellationToken ct)
    {
        var plan = await _wirdService.GetPlanAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(plan));
    }

    [HttpPut("plan")]
    public async Task<IActionResult> SetPlan(UpdateWirdPlanRequest request, CancellationToken ct)
    {
        var plan = await _wirdService.SetPlanAsync(CurrentUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(plan));
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var today = await _wirdService.GetTodayAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(today));
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteToday(CancellationToken ct)
    {
        var result = await _wirdService.CompleteTodayAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("streak")]
    public async Task<IActionResult> GetMyStreak(CancellationToken ct)
    {
        var streak = await _wirdService.GetCurrentStreakAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { currentStreak = streak }));
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

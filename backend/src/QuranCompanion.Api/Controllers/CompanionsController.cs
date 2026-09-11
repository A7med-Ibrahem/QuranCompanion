using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Companions;

namespace QuranCompanion.Api.Controllers;

[ApiController]
[Route("api/companions")]
[Authorize]
public class CompanionsController : ControllerBase
{
    private readonly ICompanionService _companionService;
    private readonly IPrivacyService _privacyService;
    private readonly IEncouragementService _encouragementService;

    public CompanionsController(
        ICompanionService companionService,
        IPrivacyService privacyService,
        IEncouragementService encouragementService)
    {
        _companionService = companionService;
        _privacyService = privacyService;
        _encouragementService = encouragementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanions(CancellationToken ct)
    {
        var companions = await _companionService.GetCompanionsAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(companions));
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncoming(CancellationToken ct)
    {
        var requests = await _companionService.GetIncomingRequestsAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(requests));
    }

    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> GetOutgoing(CancellationToken ct)
    {
        var requests = await _companionService.GetOutgoingRequestsAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(requests));
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest(SendConnectionRequestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.WirdId))
        {
            return UnprocessableEntity(ApiResponse<object>.Fail("Please enter a Wird ID.", "validation_error"));
        }

        var result = await _companionService.SendRequestAsync(CurrentUserId(), request.WirdId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{connectionId:int}/accept")]
    public async Task<IActionResult> Accept(int connectionId, CancellationToken ct)
    {
        var companion = await _companionService.AcceptAsync(CurrentUserId(), connectionId, ct);
        return Ok(ApiResponse<object>.Ok(companion));
    }

    [HttpPost("{connectionId:int}/reject")]
    public async Task<IActionResult> Reject(int connectionId, CancellationToken ct)
    {
        await _companionService.RejectAsync(CurrentUserId(), connectionId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Request declined." }));
    }

    [HttpDelete("{connectionId:int}")]
    public async Task<IActionResult> Remove(int connectionId, CancellationToken ct)
    {
        await _companionService.RemoveAsync(CurrentUserId(), connectionId, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Companion removed." }));
    }

    [HttpGet("{companionUserId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid companionUserId, CancellationToken ct)
    {
        var status = await _companionService.GetCompanionStatusAsync(CurrentUserId(), companionUserId, ct);
        return Ok(ApiResponse<object>.Ok(status));
    }

    [HttpGet("~/api/privacy-settings")]
    public async Task<IActionResult> GetPrivacySettings(CancellationToken ct)
    {
        var settings = await _privacyService.GetAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(settings));
    }

    [HttpPut("~/api/privacy-settings")]
    public async Task<IActionResult> UpdatePrivacySettings(UpdatePrivacySettingsRequest request, CancellationToken ct)
    {
        var settings = await _privacyService.UpdateAsync(CurrentUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(settings));
    }

    [HttpGet("~/api/encouragements/messages")]
    public IActionResult GetEncouragementMessages()
    {
        return Ok(ApiResponse<object>.Ok(_encouragementService.AllowedMessages));
    }

    [HttpGet("~/api/encouragements/received")]
    public async Task<IActionResult> GetReceivedEncouragements(CancellationToken ct)
    {
        var received = await _encouragementService.GetReceivedAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(received));
    }

    [HttpPost("{companionUserId:guid}/encourage")]
    public async Task<IActionResult> SendEncouragement(Guid companionUserId, SendEncouragementRequest request, CancellationToken ct)
    {
        var result = await _encouragementService.SendAsync(CurrentUserId(), companionUserId, request.Message, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

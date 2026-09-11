using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Api.Controllers;

[ApiController]
[Route("api/reading-progress")]
[Authorize]
public class ReadingProgressController : ControllerBase
{
    private readonly IReadingProgressService _service;

    public ReadingProgressController(IReadingProgressService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var progress = await _service.GetAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(progress)); // null is a valid, expected "nothing read yet" response
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMine(UpdateReadingProgressRequest request, CancellationToken ct)
    {
        var progress = await _service.UpdateAsync(CurrentUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(progress));
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

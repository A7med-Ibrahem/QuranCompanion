using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Api.Controllers;

[ApiController]
[Route("api/bookmarks")]
[Authorize]
public class BookmarksController : ControllerBase
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarksController(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var bookmarks = await _bookmarkService.GetAllAsync(CurrentUserId(), ct);
        return Ok(ApiResponse<object>.Ok(bookmarks));
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddBookmarkRequest request, CancellationToken ct)
    {
        var bookmark = await _bookmarkService.AddAsync(CurrentUserId(), request, ct);
        return Ok(ApiResponse<object>.Ok(bookmark));
    }

    [HttpDelete("{surahNumber:int}/{ayahNumber:int}")]
    public async Task<IActionResult> Remove(int surahNumber, int ayahNumber, CancellationToken ct)
    {
        await _bookmarkService.RemoveAsync(CurrentUserId(), surahNumber, ayahNumber, ct);
        return Ok(ApiResponse<object>.Ok(new { message = "Bookmark removed successfully." }));
    }

    [HttpPut("{surahNumber:int}/{ayahNumber:int}/note")]
    public async Task<IActionResult> UpdateNote(int surahNumber, int ayahNumber, UpdateBookmarkNoteRequest request, CancellationToken ct)
    {
        var bookmark = await _bookmarkService.UpdateNoteAsync(CurrentUserId(), surahNumber, ayahNumber, request, ct);
        return Ok(ApiResponse<object>.Ok(bookmark));
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

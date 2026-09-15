using Microsoft.AspNetCore.Mvc;
using QuranCompanion.Api.Common;
using QuranCompanion.Application.Common.Interfaces;

namespace QuranCompanion.Api.Controllers;

// Reading the Quran itself is public - no [Authorize] here. Personalized
// features (bookmarks, progress, wird) will sit behind auth in later phases.
[ApiController]
[Route("api/quran")]
public class QuranController : ControllerBase
{
    private readonly IQuranService _quranService;
    private readonly ITafsirService _tafsirService;
    private readonly ISearchService _searchService;

    public QuranController(IQuranService quranService, ITafsirService tafsirService, ISearchService searchService)
    {
        _quranService = quranService;
        _tafsirService = tafsirService;
        _searchService = searchService;
    }

    [HttpGet("surahs")]
    public async Task<IActionResult> GetAllSurahs(CancellationToken ct)
    {
        var surahs = await _quranService.GetAllSurahsAsync(ct);
        return Ok(ApiResponse<object>.Ok(surahs));
    }

    [HttpGet("surahs/{surahNumber:int}")]
    public async Task<IActionResult> GetSurah(int surahNumber, CancellationToken ct)
    {
        if (surahNumber is < 1 or > 114)
        {
            return BadRequest(ApiResponse<object>.Fail("Surah number must be between 1 and 114.", "invalid_surah_number"));
        }

        var surah = await _quranService.GetSurahAsync(surahNumber, ct);
        return Ok(ApiResponse<object>.Ok(surah));
    }

    [HttpGet("surahs/{surahNumber:int}/ayahs/{numberInSurah:int}")]
    public async Task<IActionResult> GetAyah(int surahNumber, int numberInSurah, CancellationToken ct)
    {
        var ayah = await _quranService.GetAyahAsync(surahNumber, numberInSurah, ct);
        return Ok(ApiResponse<object>.Ok(ayah));
    }

    [HttpGet("surahs/{surahNumber:int}/ayahs/{numberInSurah:int}/tafsir")]
    public async Task<IActionResult> GetTafsir(int surahNumber, int numberInSurah, CancellationToken ct)
    {
        var tafsir = await _tafsirService.GetAsync(surahNumber, numberInSurah, ct);
        return Ok(ApiResponse<object>.Ok(tafsir));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        var results = await _searchService.SearchAsync(q, ct);
        return Ok(ApiResponse<object>.Ok(results));
    }

    [HttpGet("page/{pageNumber:int}")]
    public async Task<IActionResult> GetPage(int pageNumber, CancellationToken ct)
    {
        var page = await _quranService.GetPageAsync(pageNumber, ct);
        return Ok(ApiResponse<object>.Ok(page));
    }

    [HttpGet("juz")]
    public async Task<IActionResult> GetJuzList(CancellationToken ct)
    {
        var juzs = await _quranService.GetJuzListAsync(ct);
        return Ok(ApiResponse<object>.Ok(juzs));
    }

    [HttpGet("juz/{juz:int}")]
    public async Task<IActionResult> GetJuz(int juz, CancellationToken ct)
    {
        var start = await _quranService.GetJuzAsync(juz, ct);
        return Ok(ApiResponse<object>.Ok(start));
    }
}

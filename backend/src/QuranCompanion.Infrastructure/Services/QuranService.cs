using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class QuranService : IQuranService
{
    private readonly AppDbContext _db;

    public QuranService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SurahSummaryDto>> GetAllSurahsAsync(CancellationToken ct = default)
    {
        var startBySurah = await GetSurahStartsAsync(ct);

        return await _db.Surahs
            .OrderBy(s => s.Number)
            .Select(s => ToSummaryDto(s, startBySurah))
            .ToListAsync(ct);
    }

    public async Task<SurahDetailDto> GetSurahAsync(int surahNumber, CancellationToken ct = default)
    {
        var surah = await _db.Surahs.FirstOrDefaultAsync(s => s.Number == surahNumber, ct)
            ?? throw new NotFoundApiException($"Surah {surahNumber} was not found.");

        var ayahs = await _db.Ayahs
            .Where(a => a.SurahNumber == surahNumber)
            .OrderBy(a => a.NumberInSurah)
            .Select(a => ToAyahDto(a))
            .ToListAsync(ct);

        if (ayahs.Count == 0)
        {
            // Metadata is always seeded, but the ayah text for this surah may not be
            // imported yet - see QuranData/README.txt for the import step.
            throw new ApiException(
                "This surah's text hasn't been imported yet. Run the Quran import to load it.",
                404, "surah_not_imported");
        }

        var startBySurah = await GetSurahStartsAsync(ct);
        return new SurahDetailDto(ToSummaryDto(surah, startBySurah), ayahs);
    }

    public async Task<AyahDto> GetAyahAsync(int surahNumber, int numberInSurah, CancellationToken ct = default)
    {
        var ayah = await _db.Ayahs
            .FirstOrDefaultAsync(a => a.SurahNumber == surahNumber && a.NumberInSurah == numberInSurah, ct)
            ?? throw new NotFoundApiException($"Ayah {surahNumber}:{numberInSurah} was not found.");

        return ToAyahDto(ayah);
    }

    public async Task<PagePositionDto> GetPageAsync(int pageNumber, CancellationToken ct = default)
    {
        if (pageNumber is < 1 or > 604)
        {
            throw new ApiException("رقم الصفحة يجب أن يكون بين ١ و ٦٠٤.", 400, "invalid_page_number");
        }

        var first = await _db.Ayahs
            .Where(a => a.Page == pageNumber)
            .OrderBy(a => a.GlobalNumber)
            .Select(a => new { a.SurahNumber, a.NumberInSurah, a.Juz })
            .FirstOrDefaultAsync(ct);

        if (first is null || first.Juz is null)
        {
            throw new ApiException(
                "بيانات صفحات المصحف غير مستوردة بعد. شغّل استيراد الصفحات والأجزاء أولًا.",
                404, "page_not_imported");
        }

        return new PagePositionDto(pageNumber, first.Juz.Value, first.SurahNumber, first.NumberInSurah);
    }

    public async Task<IReadOnlyList<JuzStartDto>> GetJuzListAsync(CancellationToken ct = default)
    {
        var juzStarts = await _db.Ayahs
            .Where(a => a.Juz >= 1 && a.Juz <= 30)
            .GroupBy(a => a.Juz!.Value)
            .Select(g => new
            {
                Juz = g.Key,
                SurahNumber = g.OrderBy(a => a.GlobalNumber).Select(a => a.SurahNumber).FirstOrDefault(),
                AyahNumber = g.OrderBy(a => a.GlobalNumber).Select(a => a.NumberInSurah).FirstOrDefault(),
                StartPage = g.OrderBy(a => a.GlobalNumber).Select(a => a.Page).FirstOrDefault()
            })
            .OrderBy(x => x.Juz)
            .ToListAsync(ct);

        if (juzStarts.Count == 0)
        {
            throw new ApiException(
                "بيانات الأجزاء غير مستوردة بعد. شغّل استيراد الصفحات والأجزاء أولًا.",
                404, "juz_not_imported");
        }

        var surahNumbers = juzStarts.Select(j => j.SurahNumber).ToList();
        var names = await _db.Surahs
            .Where(s => surahNumbers.Contains(s.Number))
            .ToDictionaryAsync(s => s.Number, s => s.ArabicName, ct);

        return juzStarts
            .Select(j => new JuzStartDto(j.Juz, j.StartPage ?? 0, j.SurahNumber, j.AyahNumber, names.GetValueOrDefault(j.SurahNumber) ?? ""))
            .ToList();
    }

    public async Task<JuzStartDto> GetJuzAsync(int juz, CancellationToken ct = default)
    {
        if (juz is < 1 or > 30)
        {
            throw new ApiException("رقم الجزء يجب أن يكون بين ١ و ٣٠.", 400, "invalid_juz_number");
        }

        var first = await _db.Ayahs
            .Where(a => a.Juz == juz)
            .OrderBy(a => a.GlobalNumber)
            .Select(a => new { a.SurahNumber, a.NumberInSurah, a.Page })
            .FirstOrDefaultAsync(ct);

        if (first is null)
        {
            throw new ApiException(
                "بيانات الأجزاء غير مستوردة بعد. شغّل استيراد الصفحات والأجزاء أولًا.",
                404, "juz_not_imported");
        }

        var name = await _db.Surahs
            .Where(s => s.Number == first.SurahNumber)
            .Select(s => s.ArabicName)
            .FirstOrDefaultAsync(ct) ?? "";

        return new JuzStartDto(juz, first.Page ?? 0, first.SurahNumber, first.NumberInSurah, name);
    }

    private async Task<Dictionary<int, (int? Page, int? Juz)>> GetSurahStartsAsync(CancellationToken ct = default)
    {
        // First ayah (canonical order) of every surah, carrying its page/juz if imported.
        var starts = await _db.Ayahs
            .GroupBy(a => a.SurahNumber)
            .Select(g => new
            {
                SurahNumber = g.Key,
                Page = g.OrderBy(a => a.GlobalNumber).Select(a => a.Page).FirstOrDefault(),
                Juz = g.OrderBy(a => a.GlobalNumber).Select(a => a.Juz).FirstOrDefault()
            })
            .ToListAsync(ct);

        return starts.ToDictionary(s => s.SurahNumber, s => (s.Page, s.Juz));
    }

    private static SurahSummaryDto ToSummaryDto(Surah s, Dictionary<int, (int? Page, int? Juz)>? startBySurah = null)
    {
        var start = startBySurah?.GetValueOrDefault(s.Number) ?? (null, null);
        return new SurahSummaryDto(
            s.Number, s.ArabicName, s.EnglishName, s.EnglishNameTranslation,
            s.NumberOfAyahs, s.RevelationType.ToString(), start.Page, start.Juz);
    }

    private static AyahDto ToAyahDto(Ayah a) => new(
        a.SurahNumber, a.NumberInSurah, a.GlobalNumber, a.Text, a.Juz, a.Page);
}

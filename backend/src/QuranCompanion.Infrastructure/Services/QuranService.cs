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
        return await _db.Surahs
            .OrderBy(s => s.Number)
            .Select(s => ToSummaryDto(s))
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

        return new SurahDetailDto(ToSummaryDto(surah), ayahs);
    }

    public async Task<AyahDto> GetAyahAsync(int surahNumber, int numberInSurah, CancellationToken ct = default)
    {
        var ayah = await _db.Ayahs
            .FirstOrDefaultAsync(a => a.SurahNumber == surahNumber && a.NumberInSurah == numberInSurah, ct)
            ?? throw new NotFoundApiException($"Ayah {surahNumber}:{numberInSurah} was not found.");

        return ToAyahDto(ayah);
    }

    private static SurahSummaryDto ToSummaryDto(Surah s) => new(
        s.Number, s.ArabicName, s.EnglishName, s.EnglishNameTranslation,
        s.NumberOfAyahs, s.RevelationType.ToString());

    private static AyahDto ToAyahDto(Ayah a) => new(
        a.SurahNumber, a.NumberInSurah, a.GlobalNumber, a.Text, a.Juz, a.Page);
}

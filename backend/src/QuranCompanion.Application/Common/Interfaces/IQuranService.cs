using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IQuranService
{
    Task<IReadOnlyList<SurahSummaryDto>> GetAllSurahsAsync(CancellationToken ct = default);
    Task<SurahDetailDto> GetSurahAsync(int surahNumber, CancellationToken ct = default);
    Task<AyahDto> GetAyahAsync(int surahNumber, int numberInSurah, CancellationToken ct = default);
}

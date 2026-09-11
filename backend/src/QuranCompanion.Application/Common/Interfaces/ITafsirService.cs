using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface ITafsirService
{
    Task<TafsirDto> GetAsync(int surahNumber, int ayahNumber, CancellationToken ct = default);
}

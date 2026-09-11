using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class ReadingProgressService : IReadingProgressService
{
    private readonly AppDbContext _db;

    public ReadingProgressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReadingProgressDto?> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var progress = await _db.ReadingProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (progress is null) return null;

        var surah = await _db.Surahs.FirstOrDefaultAsync(s => s.Number == progress.SurahNumber, ct);
        return ToDto(progress, surah?.ArabicName ?? string.Empty);
    }

    public async Task<ReadingProgressDto> UpdateAsync(Guid userId, UpdateReadingProgressRequest request, CancellationToken ct = default)
    {
        var surah = await _db.Surahs.FirstOrDefaultAsync(s => s.Number == request.SurahNumber, ct)
            ?? throw new ApiException("Invalid surah number.", 400, "invalid_surah_number");

        if (request.AyahNumber < 1 || request.AyahNumber > surah.NumberOfAyahs)
        {
            throw new ApiException("Invalid ayah number for this surah.", 400, "invalid_ayah_number");
        }

        var progress = await _db.ReadingProgresses.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (progress is null)
        {
            progress = new ReadingProgress { UserId = userId };
            _db.ReadingProgresses.Add(progress);
        }

        progress.SurahNumber = request.SurahNumber;
        progress.AyahNumber = request.AyahNumber;
        progress.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(progress, surah.ArabicName);
    }

    private static ReadingProgressDto ToDto(ReadingProgress p, string surahArabicName) => new(
        p.SurahNumber, p.AyahNumber, surahArabicName, p.UpdatedAtUtc);
}

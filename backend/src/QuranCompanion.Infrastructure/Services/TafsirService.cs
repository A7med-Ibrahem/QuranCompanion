using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class TafsirService : ITafsirService
{
    private readonly AppDbContext _db;
    private readonly ITafsirProvider _provider;
    private readonly ILogger<TafsirService> _logger;

    public TafsirService(AppDbContext db, ITafsirProvider provider, ILogger<TafsirService> logger)
    {
        _db = db;
        _provider = provider;
        _logger = logger;
    }

    public async Task<TafsirDto> GetAsync(int surahNumber, int ayahNumber, CancellationToken ct = default)
    {
        var ayahExists = await _db.Ayahs.AnyAsync(a => a.SurahNumber == surahNumber && a.NumberInSurah == ayahNumber, ct);
        if (!ayahExists)
        {
            throw new NotFoundApiException("This Ayah could not be found.");
        }

        var cached = await _db.TafsirCaches.FirstOrDefaultAsync(
            t => t.SurahNumber == surahNumber && t.AyahNumber == ayahNumber && t.TafsirId == _provider.TafsirId, ct);
        if (cached is not null)
        {
            return new TafsirDto(surahNumber, ayahNumber, cached.SourceName, cached.Text);
        }

        string? text;
        try
        {
            text = await _provider.FetchTextAsync(surahNumber, ayahNumber, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tafsir provider request failed for {Surah}:{Ayah}", surahNumber, ayahNumber);
            text = null;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ApiException("Unable to load Tafsir right now. Please try again.", 503, "tafsir_unavailable");
        }

        _db.TafsirCaches.Add(new TafsirCache
        {
            SurahNumber = surahNumber,
            AyahNumber = ayahNumber,
            TafsirId = _provider.TafsirId,
            SourceName = _provider.SourceName,
            Text = text
        });
        await _db.SaveChangesAsync(ct);

        return new TafsirDto(surahNumber, ayahNumber, _provider.SourceName, text);
    }
}

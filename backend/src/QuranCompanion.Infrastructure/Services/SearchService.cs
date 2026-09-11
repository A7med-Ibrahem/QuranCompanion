using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;
using QuranCompanion.Infrastructure.Search;

namespace QuranCompanion.Infrastructure.Services;

public class SearchService : ISearchService
{
    private const int MaxAyahResults = 50;

    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SearchResultsDto> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ApiException("Please enter a search term.", 400, "empty_search_term");
        }

        var normalizedQuery = ArabicTextNormalizer.Normalize(query.Trim());

        var matchingSurahs = await SearchSurahsAsync(query.Trim(), normalizedQuery, ct);
        var matchingAyahs = await SearchAyahsAsync(normalizedQuery, ct);

        return new SearchResultsDto(matchingSurahs, matchingAyahs);
    }

    private async Task<IReadOnlyList<SurahSummaryDto>> SearchSurahsAsync(string rawQuery, string normalizedQuery, CancellationToken ct)
    {
        var surahs = await _db.Surahs.OrderBy(s => s.Number).ToListAsync(ct);

        return surahs
            .Where(s =>
                ArabicTextNormalizer.Normalize(s.ArabicName).Contains(normalizedQuery) ||
                s.EnglishName.Contains(rawQuery, StringComparison.OrdinalIgnoreCase) ||
                s.EnglishNameTranslation.Contains(rawQuery, StringComparison.OrdinalIgnoreCase))
            .Select(s => new SurahSummaryDto(
                s.Number, s.ArabicName, s.EnglishName, s.EnglishNameTranslation, s.NumberOfAyahs, s.RevelationType.ToString()))
            .ToList();
    }

    private async Task<IReadOnlyList<AyahSearchResultDto>> SearchAyahsAsync(string normalizedQuery, CancellationToken ct)
    {
        // The normalization is done in the app, but the substring match itself
        // runs in SQL against the precomputed NormalizedText column - fast even
        // across all 6236 ayahs, no separate search engine needed.
        var matches = await _db.Ayahs
            .Where(a => a.NormalizedText != null && a.NormalizedText.Contains(normalizedQuery))
            .OrderBy(a => a.GlobalNumber)
            .Take(MaxAyahResults)
            .ToListAsync(ct);

        if (matches.Count == 0) return Array.Empty<AyahSearchResultDto>();

        var surahNames = await _db.Surahs
            .Where(s => matches.Select(m => m.SurahNumber).Distinct().Contains(s.Number))
            .ToDictionaryAsync(s => s.Number, s => s.ArabicName, ct);

        var results = new List<AyahSearchResultDto>(matches.Count);
        foreach (var ayah in matches)
        {
            var (start, length) = LocateOriginalMatch(ayah.Text, normalizedQuery);
            results.Add(new AyahSearchResultDto(
                ayah.SurahNumber, surahNames[ayah.SurahNumber], ayah.NumberInSurah, ayah.Text, start, length));
        }

        return results;
    }

    /// <summary>Maps the match found in normalized text back to a highlight range in the original (vocalized) text.</summary>
    private static (int? Start, int? Length) LocateOriginalMatch(string originalText, string normalizedQuery)
    {
        var (normalized, map) = ArabicTextNormalizer.NormalizeWithMap(originalText);
        var index = normalized.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (index < 0 || normalizedQuery.Length == 0) return (null, null);

        var originalStart = map[index];
        var originalEndExclusive = map[index + normalizedQuery.Length - 1] + 1;
        return (originalStart, originalEndExclusive - originalStart);
    }
}

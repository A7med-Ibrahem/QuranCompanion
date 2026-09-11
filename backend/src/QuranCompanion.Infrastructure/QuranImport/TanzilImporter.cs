using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;
using QuranCompanion.Infrastructure.Search;

namespace QuranCompanion.Infrastructure.QuranImport;

/// <summary>
/// Imports the full Quran text from the official Tanzil.net "Simple text"
/// export (Uthmani script), format: one ayah per line, "surah|ayah|text".
/// Comment lines start with '#' and are skipped. Idempotent - already
/// imported (surah, ayah) pairs are skipped, so it's safe to re-run.
///
/// Get the file from: https://tanzil.net/download/ - choose:
///   Text Type: "Uthmani"
///   Format: "Text (with aya numbers)" -> simple text, one ayah per line
/// </summary>
public static class TanzilImporter
{
    public record ImportResult(int Inserted, int Skipped, int TotalLinesRead);

    public static async Task<ImportResult> ImportAsync(AppDbContext db, string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Quran text file not found at: {filePath}");
        }

        var surahAyahCounts = await db.Surahs
            .OrderBy(s => s.Number)
            .Select(s => new { s.Number, s.NumberOfAyahs })
            .ToListAsync(ct);

        if (surahAyahCounts.Count != 114)
        {
            throw new InvalidOperationException(
                "Surahs metadata isn't fully seeded (expected 114 rows). Run the app once in Development first so QuranSeeder can seed it, then re-run the import.");
        }

        // Precompute the global (1-6236) ayah number each (surah, ayah) maps to.
        var globalStart = new Dictionary<int, int>();
        var running = 1;
        foreach (var s in surahAyahCounts)
        {
            globalStart[s.Number] = running;
            running += s.NumberOfAyahs;
        }

        var existing = await db.Ayahs
            .Select(a => new { a.SurahNumber, a.NumberInSurah })
            .ToListAsync(ct);
        var existingSet = existing.Select(e => (e.SurahNumber, e.NumberInSurah)).ToHashSet();

        var toInsert = new List<Ayah>();
        var totalLines = 0;
        var skipped = 0;

        foreach (var rawLine in await File.ReadAllLinesAsync(filePath, ct))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var parts = line.Split('|', 3);
            if (parts.Length != 3) continue; // ignore malformed/trailing lines

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var surahNum)) continue;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ayahNum)) continue;
            var text = parts[2].Trim();
            if (text.Length == 0) continue;

            totalLines++;

            if (existingSet.Contains((surahNum, ayahNum)))
            {
                skipped++;
                continue;
            }

            if (!globalStart.TryGetValue(surahNum, out var start)) continue; // unknown surah number, ignore

            toInsert.Add(new Ayah
            {
                SurahNumber = surahNum,
                NumberInSurah = ayahNum,
                GlobalNumber = start + ayahNum - 1,
                Text = text,
                NormalizedText = ArabicTextNormalizer.Normalize(text)
            });
        }

        if (toInsert.Count > 0)
        {
            db.Ayahs.AddRange(toInsert);
            await db.SaveChangesAsync(ct);
        }

        return new ImportResult(toInsert.Count, skipped, totalLines);
    }
}

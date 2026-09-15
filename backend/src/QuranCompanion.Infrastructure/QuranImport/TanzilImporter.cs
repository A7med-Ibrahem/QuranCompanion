using System.Globalization;
using System.Text.Json;
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

    public record PageJuzResult(int RowsRead, int AlreadyCorrect, int Updated);

    /// <summary>
    /// Writes the Mushaf 604-page / 30-juz numbers onto the already-imported
    /// Ayah rows using a verified mapping file (QuranData/pages-juz.json,
    /// generated from mjmirza/quran-dataset and cross-checked against Tanzil
    /// quran-data.xml, alquran.cloud, and this project's quran-uthmani.txt).
    ///
    /// Safe to re-run: identical values are left untouched. Before it will do
    /// anything the ayah text must already be imported (its (surah, ayah) keys
    /// must match the mapping 1:1, otherwise it throws without writing).
    /// </summary>
    public static async Task<PageJuzResult> ImportPageJuzAsync(AppDbContext db, string jsonPath, CancellationToken ct = default)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Pages/juz mapping file not found at: {jsonPath}");
        }

        var surahCount = await db.Surahs.CountAsync(ct);
        if (surahCount != 114)
        {
            throw new InvalidOperationException(
                "Surahs metadata isn't fully seeded (expected 114 rows). Run the app once in Development so QuranSeeder can seed it, then re-run the import.");
        }

        var existing = await db.Ayahs
            .Select(a => new { a.SurahNumber, a.NumberInSurah })
            .ToListAsync(ct);
        if (existing.Count == 0)
        {
            throw new InvalidOperationException(
                "No ayah text is imported yet. Run `import-quran <quran-uthmani.txt>` first, then re-run this import.");
        }

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath, ct));

        var mapping = new List<(int Surah, int Ayah, int Page, int Juz)>();
        foreach (var row in doc.RootElement.GetProperty("rows").EnumerateArray())
        {
            mapping.Add((row[0].GetInt32(), row[1].GetInt32(), row[2].GetInt32(), row[3].GetInt32()));
        }

        if (mapping.Count != 6236)
        {
            throw new InvalidOperationException($"Mapping has {mapping.Count} rows, expected exactly 6236. Refusing to import.");
        }

        foreach (var (surah, ayah, page, juz) in mapping)
        {
            if (surah is < 1 or > 114 || ayah < 1 || page is < 1 or > 604 || juz is < 1 or > 30)
            {
                throw new InvalidOperationException(
                    $"Mapping row out of range: surah={surah}, ayah={ayah}, page={page}, juz={juz}. Refusing to import.");
            }
        }

        var existingSet = existing.Select(e => (e.SurahNumber, e.NumberInSurah)).ToHashSet();
        var mappingSet = mapping.Select(m => (m.Surah, m.Ayah)).ToHashSet();

        var mappingOnly = mappingSet.Except(existingSet).ToList();
        var existingOnly = existingSet.Except(mappingSet).ToList();
        if (mappingOnly.Count != 0)
        {
            throw new InvalidOperationException(
                $"Mapping references {mappingOnly.Count} (surah, ayah) pairs that are missing from the DB (first: {string.Join(", ", mappingOnly.Take(5))}). The ayah text import may be stale. Refusing to write.");
        }
        if (existingOnly.Count != 0)
        {
            throw new InvalidOperationException(
                $"DB contains {existingOnly.Count} (surah, ayah) pairs with no mapping row (first: {string.Join(", ", existingOnly.Take(5))}). Refusing to write.");
        }

        var byKey = await db.Ayahs.ToDictionaryAsync(a => (a.SurahNumber, a.NumberInSurah), ct);

        var alreadyCorrect = 0;
        var updated = 0;
        foreach (var (surah, ayah, page, juz) in mapping)
        {
            var ayahRow = byKey[(surah, ayah)];
            if (ayahRow.Juz == juz && ayahRow.Page == page)
            {
                alreadyCorrect++;
                continue;
            }

            ayahRow.Juz = juz;
            ayahRow.Page = page;
            updated++;
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return new PageJuzResult(mapping.Count, alreadyCorrect, updated);
    }
}

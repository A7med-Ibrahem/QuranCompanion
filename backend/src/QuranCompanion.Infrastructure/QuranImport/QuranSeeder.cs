using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;
using QuranCompanion.Infrastructure.Search;

namespace QuranCompanion.Infrastructure.QuranImport;

/// <summary>
/// Runs once at Development startup (see Program.cs). Seeds the 114-surah
/// metadata table (always) and a small Al-Fatiha starter set (only if the
/// Ayahs table is completely empty), so the reader has real content to show
/// immediately. The remaining ~6229 ayahs come from TanzilImporter.
/// </summary>
public static class QuranSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedSurahsAsync(db, ct);
        await SeedStarterAyahsAsync(db, ct);
        await BackfillNormalizedTextAsync(db, ct);
    }

    /// <summary>
    /// Fills in NormalizedText for any Ayah rows that predate the search
    /// feature (e.g. already-imported ayahs from before this column existed).
    /// Safe to run every startup - only touches rows where it's still null.
    /// </summary>
    private static async Task BackfillNormalizedTextAsync(AppDbContext db, CancellationToken ct)
    {
        var pending = await db.Ayahs.Where(a => a.NormalizedText == null).ToListAsync(ct);
        if (pending.Count == 0) return;

        foreach (var ayah in pending)
        {
            ayah.NormalizedText = ArabicTextNormalizer.Normalize(ayah.Text);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSurahsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Surahs.AnyAsync(ct)) return;

        var path = Path.Combine(AppContext.BaseDirectory, "QuranData", "surahs.json");
        if (!File.Exists(path))
        {
            // Fallback for `dotnet run` where content files sit next to the .csproj, not the output dir.
            path = Path.Combine(FindInfrastructureProjectDir(), "QuranData", "surahs.json");
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var records = JsonSerializer.Deserialize<List<SurahSeedRecord>>(json, JsonOpts)
            ?? throw new InvalidOperationException("surahs.json could not be parsed.");

        var surahs = records.Select(r => new Surah
        {
            Number = r.Number,
            ArabicName = r.Name,
            EnglishName = r.EnglishName,
            EnglishNameTranslation = r.EnglishNameTranslation,
            NumberOfAyahs = r.NumberOfAyahs,
            RevelationType = r.RevelationType.Equals("Medinan", StringComparison.OrdinalIgnoreCase)
                ? RevelationType.Medinan
                : RevelationType.Meccan
        });

        db.Surahs.AddRange(surahs);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedStarterAyahsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Ayahs.AnyAsync(ct)) return;

        var path = Path.Combine(AppContext.BaseDirectory, "QuranData", "ayahs-seed.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(FindInfrastructureProjectDir(), "QuranData", "ayahs-seed.json");
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var records = JsonSerializer.Deserialize<List<AyahSeedRecord>>(json, JsonOpts)
            ?? throw new InvalidOperationException("ayahs-seed.json could not be parsed.");

        var ayahs = records.Select(r => new Ayah
        {
            SurahNumber = r.Surah,
            NumberInSurah = r.Ayah,
            GlobalNumber = r.Ayah, // correct only within surah 1; TanzilImporter recomputes properly for the rest
            Text = r.Text,
            NormalizedText = ArabicTextNormalizer.Normalize(r.Text)
        });

        db.Ayahs.AddRange(ayahs);
        await db.SaveChangesAsync(ct);
    }

    private static string FindInfrastructureProjectDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "QuranData")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the QuranData folder from the current directory.");
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private record SurahSeedRecord(int Number, string Name, string EnglishName, string EnglishNameTranslation, int NumberOfAyahs, string RevelationType);
    private record AyahSeedRecord(int Surah, int Ayah, string Text);
}

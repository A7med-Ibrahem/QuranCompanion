namespace QuranCompanion.Domain.Entities;

/// <summary>
/// One of the 114 chapters of the Quran. Metadata only - the Arabic text
/// itself lives in Ayah. Never mutate Name/ArabicName after seeding; the
/// Quran's structure is fixed and must match the trusted source exactly.
/// </summary>
public class Surah
{
    /// <summary>1-114, matches the canonical Mushaf order.</summary>
    public int Number { get; set; }

    /// <summary>Full Arabic name as it appears in the Mushaf, e.g. سُورَةُ ٱلْفَاتِحَةِ</summary>
    public string ArabicName { get; set; } = default!;

    /// <summary>Standard English transliteration, e.g. "Al-Faatiha".</summary>
    public string EnglishName { get; set; } = default!;

    /// <summary>English meaning, e.g. "The Opening".</summary>
    public string EnglishNameTranslation { get; set; } = default!;

    public int NumberOfAyahs { get; set; }

    public RevelationType RevelationType { get; set; }

    public ICollection<Ayah> Ayahs { get; set; } = new List<Ayah>();
}

public enum RevelationType
{
    Meccan = 0,
    Medinan = 1
}

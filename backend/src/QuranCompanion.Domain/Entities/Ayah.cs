namespace QuranCompanion.Domain.Entities;

/// <summary>
/// A single verse. Text is stored exactly as sourced (Tanzil Uthmani script)
/// and must never be edited, normalized, or altered in place - see spec
/// section 3 ("the original Quran text must never be modified"). Search
/// normalization happens at query time, not by mutating this field.
/// </summary>
public class Ayah
{
    public int Id { get; set; }

    public int SurahNumber { get; set; }
    public Surah Surah { get; set; } = default!;

    /// <summary>1-based position within the surah.</summary>
    public int NumberInSurah { get; set; }

    /// <summary>1-6236, position across the whole Quran.</summary>
    public int GlobalNumber { get; set; }

    public string Text { get; set; } = default!;

    /// <summary>
    /// Search-only normalized form of Text: diacritics stripped, alef/ya
    /// variants unified (spec section 9). Never shown to the user - the
    /// reader always renders Text exactly as imported.
    /// </summary>
    public string? NormalizedText { get; set; }

    /// <summary>1-30. Null until the full Tanzil import runs (see QuranData/README.txt).</summary>
    public int? Juz { get; set; }

    /// <summary>1-604 in the standard Madani Mushaf pagination. Null until full import.</summary>
    public int? Page { get; set; }
}

namespace QuranCompanion.Domain.Entities;

/// <summary>
/// Local cache of Tafsir text fetched from an external provider (spec section 10).
/// Avoids re-fetching on every request and gives the reader something to show
/// even if the external Tafsir API is briefly unreachable. Never displayed as
/// a replacement for the Ayah text itself - always shown clearly separated
/// with its source attributed (spec: "never invent or generate" Tafsir).
/// </summary>
public class TafsirCache
{
    public int Id { get; set; }

    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }

    /// <summary>Identifies which Tafsir book/source this text is from (see ITafsirProvider).</summary>
    public int TafsirId { get; set; }
    public string SourceName { get; set; } = default!;

    public string Text { get; set; } = default!;

    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
}

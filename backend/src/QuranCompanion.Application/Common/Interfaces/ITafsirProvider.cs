namespace QuranCompanion.Application.Common.Interfaces;

/// <summary>
/// Abstraction over a single external Tafsir source. Add another
/// implementation (e.g. a different book/provider) and register it without
/// touching ITafsirService, the controller, or the frontend contract - this
/// is the "architecture so additional Tafsir sources can be added" spec asks for.
/// </summary>
public interface ITafsirProvider
{
    int TafsirId { get; }
    string SourceName { get; }
    Task<string?> FetchTextAsync(int surahNumber, int ayahNumber, CancellationToken ct = default);
}

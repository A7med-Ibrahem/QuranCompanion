using System.Net.Http.Json;
using QuranCompanion.Application.Common.Interfaces;

namespace QuranCompanion.Infrastructure.Services;

/// <summary>
/// Tafsir Al-Muyassar (التفسير الميسر) via the free, unauthenticated
/// api.quran-tafseer.com service. Chosen as the default source: widely used,
/// modern standard Arabic, produced by a recognized panel of scholars.
/// </summary>
public class QuranTafseerComProvider : ITafsirProvider
{
    public int TafsirId => 1;
    public string SourceName => "التفسير الميسر";

    private readonly HttpClient _http;

    public QuranTafseerComProvider(HttpClient http)
    {
        _http = http;
        _http.BaseAddress ??= new Uri("http://api.quran-tafseer.com/");
    }

    public async Task<string?> FetchTextAsync(int surahNumber, int ayahNumber, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"tafseer/{TafsirId}/{surahNumber}/{ayahNumber}", ct);
        if (!response.IsSuccessStatusCode) return null;

        var payload = await response.Content.ReadFromJsonAsync<TafseerResponse>(cancellationToken: ct);
        return string.IsNullOrWhiteSpace(payload?.text) ? null : payload.text;
    }

    private record TafseerResponse(int tafseer_id, string tafseer_name, string ayah_url, int ayah_number, string text);
}

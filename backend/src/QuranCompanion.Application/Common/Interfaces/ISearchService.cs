using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface ISearchService
{
    Task<SearchResultsDto> SearchAsync(string query, CancellationToken ct = default);
}

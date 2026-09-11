using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IBookmarkService
{
    Task<IReadOnlyList<BookmarkDto>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<BookmarkDto> AddAsync(Guid userId, AddBookmarkRequest request, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, int surahNumber, int ayahNumber, CancellationToken ct = default);
    Task<BookmarkDto> UpdateNoteAsync(Guid userId, int surahNumber, int ayahNumber, UpdateBookmarkNoteRequest request, CancellationToken ct = default);
}

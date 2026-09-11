using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class BookmarkService : IBookmarkService
{
    private readonly AppDbContext _db;

    public BookmarkService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BookmarkDto>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var bookmarks = await _db.Bookmarks
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(ct);

        if (bookmarks.Count == 0) return Array.Empty<BookmarkDto>();

        var result = new List<BookmarkDto>(bookmarks.Count);
        foreach (var b in bookmarks)
        {
            result.Add(await ToDtoAsync(b, ct));
        }
        return result;
    }

    public async Task<BookmarkDto> AddAsync(Guid userId, AddBookmarkRequest request, CancellationToken ct = default)
    {
        var ayahExists = await _db.Ayahs.AnyAsync(
            a => a.SurahNumber == request.SurahNumber && a.NumberInSurah == request.AyahNumber, ct);
        if (!ayahExists)
        {
            throw new ApiException("This Ayah could not be found.", 404, "ayah_not_found");
        }

        var duplicate = await _db.Bookmarks.AnyAsync(
            b => b.UserId == userId && b.SurahNumber == request.SurahNumber && b.AyahNumber == request.AyahNumber, ct);
        if (duplicate)
        {
            throw new ApiException("This Ayah is already bookmarked.", 409, "already_bookmarked");
        }

        var bookmark = new Bookmark
        {
            UserId = userId,
            SurahNumber = request.SurahNumber,
            AyahNumber = request.AyahNumber,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _db.Bookmarks.Add(bookmark);
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(bookmark, ct);
    }

    public async Task RemoveAsync(Guid userId, int surahNumber, int ayahNumber, CancellationToken ct = default)
    {
        var bookmark = await _db.Bookmarks.FirstOrDefaultAsync(
            b => b.UserId == userId && b.SurahNumber == surahNumber && b.AyahNumber == ayahNumber, ct)
            ?? throw new NotFoundApiException("Bookmark not found.");

        _db.Bookmarks.Remove(bookmark);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BookmarkDto> UpdateNoteAsync(Guid userId, int surahNumber, int ayahNumber, UpdateBookmarkNoteRequest request, CancellationToken ct = default)
    {
        var bookmark = await _db.Bookmarks.FirstOrDefaultAsync(
            b => b.UserId == userId && b.SurahNumber == surahNumber && b.AyahNumber == ayahNumber, ct)
            ?? throw new NotFoundApiException("Bookmark not found.");

        bookmark.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(bookmark, ct);
    }

    private async Task<BookmarkDto> ToDtoAsync(Bookmark b, CancellationToken ct)
    {
        var ayah = await _db.Ayahs.FirstAsync(a => a.SurahNumber == b.SurahNumber && a.NumberInSurah == b.AyahNumber, ct);
        var surahName = await _db.Surahs.Where(s => s.Number == b.SurahNumber).Select(s => s.ArabicName).FirstAsync(ct);

        return new BookmarkDto(b.SurahNumber, surahName, b.AyahNumber, ayah.Text, b.Note, b.CreatedAtUtc);
    }
}

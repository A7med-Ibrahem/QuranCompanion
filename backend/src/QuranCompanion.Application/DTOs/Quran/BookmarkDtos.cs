namespace QuranCompanion.Application.DTOs.Quran;

public record BookmarkDto(
    int SurahNumber,
    string SurahArabicName,
    int AyahNumber,
    string AyahText,
    string? Note,
    DateTime CreatedAtUtc
);

public record AddBookmarkRequest(int SurahNumber, int AyahNumber, string? Note);
public record UpdateBookmarkNoteRequest(string? Note);

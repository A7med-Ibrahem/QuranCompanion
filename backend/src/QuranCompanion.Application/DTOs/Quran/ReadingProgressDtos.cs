namespace QuranCompanion.Application.DTOs.Quran;

public record ReadingProgressDto(
    int SurahNumber,
    int AyahNumber,
    string SurahArabicName,
    DateTime UpdatedAtUtc
);

public record UpdateReadingProgressRequest(int SurahNumber, int AyahNumber);

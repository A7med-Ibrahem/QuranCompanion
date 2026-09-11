namespace QuranCompanion.Application.DTOs.Quran;

public record SurahSummaryDto(
    int Number,
    string ArabicName,
    string EnglishName,
    string EnglishNameTranslation,
    int NumberOfAyahs,
    string RevelationType
);

public record AyahDto(
    int SurahNumber,
    int NumberInSurah,
    int GlobalNumber,
    string Text,
    int? Juz,
    int? Page
);

public record SurahDetailDto(
    SurahSummaryDto Surah,
    IReadOnlyList<AyahDto> Ayahs
);

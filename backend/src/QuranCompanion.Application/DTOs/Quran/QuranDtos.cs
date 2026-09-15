namespace QuranCompanion.Application.DTOs.Quran;

public record SurahSummaryDto(
    int Number,
    string ArabicName,
    string EnglishName,
    string EnglishNameTranslation,
    int NumberOfAyahs,
    string RevelationType,
    int? StartPage = null,
    int? StartJuz = null
);

/// <summary>
/// Where a Mushaf page begins - the first ayah of that page plus its juz.
/// Populated from the imported per-ayah Page/Juz data.
/// </summary>
public record PagePositionDto(
    int PageNumber,
    int Juz,
    int SurahNumber,
    int AyahNumber
);

/// <summary>
/// Where a juz begins - first ayah of the juz plus the page it starts on.
/// </summary>
public record JuzStartDto(
    int Juz,
    int StartPage,
    int SurahNumber,
    int AyahNumber,
    string SurahArabicName
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

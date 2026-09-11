namespace QuranCompanion.Application.DTOs.Quran;

public record AyahSearchResultDto(
    int SurahNumber,
    string SurahArabicName,
    int AyahNumber,
    string Text,
    int? MatchStart,   // character offset into Text where the match begins - null if not found (e.g. multi-word query)
    int? MatchLength
);

public record SearchResultsDto(
    IReadOnlyList<SurahSummaryDto> MatchingSurahs,
    IReadOnlyList<AyahSearchResultDto> MatchingAyahs
);

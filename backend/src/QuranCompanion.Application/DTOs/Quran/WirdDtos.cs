namespace QuranCompanion.Application.DTOs.Quran;

public record WirdPlanDto(
    string Type,
    int? CustomAyahsPerDay,
    DateOnly? TargetCompletionDate,
    DateTime UpdatedAtUtc
);

public record UpdateWirdPlanRequest(string Type, int? CustomAyahsPerDay, DateOnly? TargetCompletionDate);

public record TodayWirdDto(
    string Type,
    int StartSurah,
    string StartSurahName,
    int StartAyah,
    int EndSurah,
    string EndSurahName,
    int EndAyah,
    int AyahCount,
    bool IsCompletedToday,

    /// <summary>Only set when the plan is GoalBased - how many days are left until the target date.</summary>
    int? DaysRemainingInGoal,
    DateOnly? TargetCompletionDate
);

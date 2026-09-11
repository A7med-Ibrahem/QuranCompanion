namespace QuranCompanion.Application.DTOs.Companions;

public record PrivacySettingsDto(
    bool ShareCompletionStatus,
    bool ShareStreak,
    bool ShareWirdRange,
    bool ShareReadingProgress
);

public record UpdatePrivacySettingsRequest(
    bool ShareCompletionStatus,
    bool ShareStreak,
    bool ShareWirdRange,
    bool ShareReadingProgress
);

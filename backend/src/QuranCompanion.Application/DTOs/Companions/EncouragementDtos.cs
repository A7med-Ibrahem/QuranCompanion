namespace QuranCompanion.Application.DTOs.Companions;

public record SendEncouragementRequest(string Message);

public record EncouragementDto(
    int Id,
    Guid FromUserId,
    string FromDisplayName,
    string Message,
    DateTime CreatedAtUtc
);

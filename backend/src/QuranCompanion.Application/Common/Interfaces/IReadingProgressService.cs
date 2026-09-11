using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IReadingProgressService
{
    /// <summary>Null if the user hasn't read anything yet - no "Continue Reading" card to show.</summary>
    Task<ReadingProgressDto?> GetAsync(Guid userId, CancellationToken ct = default);

    Task<ReadingProgressDto> UpdateAsync(Guid userId, UpdateReadingProgressRequest request, CancellationToken ct = default);
}

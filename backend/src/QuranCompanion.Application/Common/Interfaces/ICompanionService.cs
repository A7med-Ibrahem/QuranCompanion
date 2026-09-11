using QuranCompanion.Application.DTOs.Companions;
using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface ICompanionService
{
    Task<ConnectionRequestDto> SendRequestAsync(Guid userId, string wirdId, CancellationToken ct = default);
    Task<CompanionDto> AcceptAsync(Guid userId, int connectionId, CancellationToken ct = default);
    Task RejectAsync(Guid userId, int connectionId, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, int connectionId, CancellationToken ct = default);

    Task<IReadOnlyList<CompanionDto>> GetCompanionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequestDto>> GetIncomingRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<ConnectionRequestDto>> GetOutgoingRequestsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>What the viewer is allowed to see about a companion's Wird activity, per the companion's own privacy settings.</summary>
    Task<CompanionStatusDto> GetCompanionStatusAsync(Guid viewerId, Guid companionUserId, CancellationToken ct = default);
}

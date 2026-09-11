using QuranCompanion.Application.DTOs.Companions;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IEncouragementService
{
    /// <summary>The fixed set of messages a person may send - no free text (spec section 7).</summary>
    IReadOnlyList<string> AllowedMessages { get; }

    Task<EncouragementDto> SendAsync(Guid fromUserId, Guid toUserId, string message, CancellationToken ct = default);
    Task<IReadOnlyList<EncouragementDto>> GetReceivedAsync(Guid userId, CancellationToken ct = default);
}

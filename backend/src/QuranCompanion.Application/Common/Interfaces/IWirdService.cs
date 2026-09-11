using QuranCompanion.Application.DTOs.Quran;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IWirdService
{
    Task<WirdPlanDto?> GetPlanAsync(Guid userId, CancellationToken ct = default);
    Task<WirdPlanDto> SetPlanAsync(Guid userId, UpdateWirdPlanRequest request, CancellationToken ct = default);
    Task<TodayWirdDto?> GetTodayAsync(Guid userId, CancellationToken ct = default);
    Task<TodayWirdDto> CompleteTodayAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Consecutive days (ending today or yesterday) this user completed their Wird. 0 if the streak is broken.</summary>
    Task<int> GetCurrentStreakAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Consecutive days (ending today or yesterday) BOTH users completed their Wird.</summary>
    Task<int> GetSharedStreakAsync(Guid userIdA, Guid userIdB, CancellationToken ct = default);
}

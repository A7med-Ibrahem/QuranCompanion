using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Exceptions;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Quran;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;
using QuranCompanion.Infrastructure.Companions;

namespace QuranCompanion.Infrastructure.Services;

public class WirdService : IWirdService
{
    private const int TotalAyahs = 6236;

    private readonly AppDbContext _db;

    public WirdService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<WirdPlanDto?> GetPlanAsync(Guid userId, CancellationToken ct = default)
    {
        var plan = await _db.WirdPlans.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return plan is null ? null : ToPlanDto(plan);
    }

    public async Task<WirdPlanDto> SetPlanAsync(Guid userId, UpdateWirdPlanRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<WirdType>(request.Type, ignoreCase: true, out var type))
        {
            throw new ApiException(
                "Unknown Wird type. Choose one of: OnePage, FivePages, TenPages, QuarterJuz, HalfJuz, OneJuz, Custom, GoalBased.",
                400, "invalid_wird_type");
        }

        if (type == WirdType.Custom && (request.CustomAyahsPerDay is null or < 1))
        {
            throw new ApiException("Custom Wird needs a positive number of ayahs per day.", 400, "invalid_custom_wird");
        }

        if (type == WirdType.GoalBased)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (request.TargetCompletionDate is null || request.TargetCompletionDate <= today)
            {
                throw new ApiException("Please choose a target date in the future.", 400, "invalid_target_date");
            }
        }

        var plan = await _db.WirdPlans.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (plan is null)
        {
            plan = new WirdPlan { UserId = userId };
            _db.WirdPlans.Add(plan);
        }

        plan.Type = type;
        plan.CustomAyahsPerDay = type == WirdType.Custom ? request.CustomAyahsPerDay : null;
        plan.TargetCompletionDate = type == WirdType.GoalBased ? request.TargetCompletionDate : null;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToPlanDto(plan);
    }

    public async Task<TodayWirdDto?> GetTodayAsync(Guid userId, CancellationToken ct = default)
    {
        var plan = await _db.WirdPlans.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (plan is null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todaysCompletion = await _db.WirdCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CompletionDate == today, ct);

        if (todaysCompletion is not null)
        {
            return await BuildDtoAsync(plan, todaysCompletion.StartGlobalAyah, todaysCompletion.EndGlobalAyah, isCompleted: true, ct);
        }

        var (start, end) = await ComputeRangeAsync(userId, plan, ct);
        return await BuildDtoAsync(plan, start, end, isCompleted: false, ct);
    }

    public async Task<TodayWirdDto> CompleteTodayAsync(Guid userId, CancellationToken ct = default)
    {
        var plan = await _db.WirdPlans.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new ApiException("Set a daily Wird before completing it.", 400, "no_wird_plan");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await _db.WirdCompletions
            .FirstOrDefaultAsync(c => c.UserId == userId && c.CompletionDate == today, ct);
        if (existing is not null)
        {
            return await BuildDtoAsync(plan, existing.StartGlobalAyah, existing.EndGlobalAyah, isCompleted: true, ct);
        }

        var (start, end) = await ComputeRangeAsync(userId, plan, ct);

        _db.WirdCompletions.Add(new WirdCompletion
        {
            UserId = userId,
            CompletionDate = today,
            StartGlobalAyah = start,
            EndGlobalAyah = end
        });
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(plan, start, end, isCompleted: true, ct);
    }

    private async Task<(int start, int end)> ComputeRangeAsync(Guid userId, WirdPlan plan, CancellationToken ct)
    {
        var lastCompletion = await _db.WirdCompletions
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CompletionDate)
            .FirstOrDefaultAsync(ct);

        var start = lastCompletion is null ? 1 : lastCompletion.EndGlobalAyah + 1;
        if (start > TotalAyahs) start = 1;

        var ayahsPerDay = AyahsPerDay(plan, start);
        var end = Math.Min(start + ayahsPerDay - 1, TotalAyahs);

        return (start, end);
    }

    /// <summary>
    /// For GoalBased plans this is recomputed fresh every call from the ayahs
    /// still remaining and days still remaining until the target date - so if
    /// yesterday was missed, today's portion automatically grows to keep the
    /// goal on track, rather than silently falling behind (spec section 16's
    /// "adjust if you fall behind", applied to any goal, not just Ramadan).
    /// </summary>
    private static int AyahsPerDay(WirdPlan plan, int startGlobalAyah)
    {
        if (plan.Type == WirdType.GoalBased && plan.TargetCompletionDate is { } target)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var remainingDays = Math.Max(1, target.DayNumber - today.DayNumber + 1);
            var remainingAyahs = Math.Max(1, TotalAyahs - startGlobalAyah + 1);
            return (int)Math.Ceiling(remainingAyahs / (double)remainingDays);
        }

        return plan.Type switch
        {
            WirdType.OnePage => 10,
            WirdType.FivePages => 50,
            WirdType.TenPages => 100,
            WirdType.QuarterJuz => 52,
            WirdType.HalfJuz => 104,
            WirdType.OneJuz => 208,
            WirdType.Custom => plan.CustomAyahsPerDay ?? 10,
            _ => 10
        };
    }

    public async Task<int> GetCurrentStreakAsync(Guid userId, CancellationToken ct = default)
    {
        var dates = await _db.WirdCompletions
            .Where(c => c.UserId == userId)
            .Select(c => c.CompletionDate)
            .ToListAsync(ct);

        return StreakCalculator.Compute(dates.ToHashSet(), DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public async Task<int> GetSharedStreakAsync(Guid userIdA, Guid userIdB, CancellationToken ct = default)
    {
        var datesA = await _db.WirdCompletions.Where(c => c.UserId == userIdA).Select(c => c.CompletionDate).ToListAsync(ct);
        var datesB = await _db.WirdCompletions.Where(c => c.UserId == userIdB).Select(c => c.CompletionDate).ToListAsync(ct);

        return StreakCalculator.ComputeShared(datesA.ToHashSet(), datesB.ToHashSet(), DateOnly.FromDateTime(DateTime.UtcNow));
    }

    private async Task<TodayWirdDto> BuildDtoAsync(WirdPlan plan, int startGlobal, int endGlobal, bool isCompleted, CancellationToken ct)
    {
        var startAyah = await _db.Ayahs.FirstAsync(a => a.GlobalNumber == startGlobal, ct);
        var endAyah = await _db.Ayahs.FirstAsync(a => a.GlobalNumber == endGlobal, ct);

        var startSurahName = await _db.Surahs.Where(s => s.Number == startAyah.SurahNumber).Select(s => s.ArabicName).FirstAsync(ct);
        var endSurahName = startAyah.SurahNumber == endAyah.SurahNumber
            ? startSurahName
            : await _db.Surahs.Where(s => s.Number == endAyah.SurahNumber).Select(s => s.ArabicName).FirstAsync(ct);

        int? daysRemaining = null;
        if (plan.Type == WirdType.GoalBased && plan.TargetCompletionDate is { } target)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            daysRemaining = Math.Max(0, target.DayNumber - today.DayNumber);
        }

        return new TodayWirdDto(
            plan.Type.ToString(),
            startAyah.SurahNumber, startSurahName, startAyah.NumberInSurah,
            endAyah.SurahNumber, endSurahName, endAyah.NumberInSurah,
            endGlobal - startGlobal + 1,
            isCompleted,
            daysRemaining,
            plan.Type == WirdType.GoalBased ? plan.TargetCompletionDate : null);
    }

    private static WirdPlanDto ToPlanDto(WirdPlan plan) =>
        new(plan.Type.ToString(), plan.CustomAyahsPerDay, plan.TargetCompletionDate, plan.UpdatedAtUtc);
}

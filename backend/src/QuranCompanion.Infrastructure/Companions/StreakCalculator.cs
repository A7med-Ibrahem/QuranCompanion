namespace QuranCompanion.Infrastructure.Companions;

/// <summary>
/// Turns a set of "Wird completed" dates into a current streak count (spec
/// section 8). A streak stays "alive" through today even if today isn't
/// done yet - it only breaks once a full day passes with nothing completed.
///
/// Includes a monthly "streak freeze" allowance (like the grace mechanic in
/// popular habit-tracking apps): each user gets up to <see cref="FreeRestoresPerMonth"/>
/// missed days per calendar month that don't break their streak. The 6th miss
/// within the same month breaks it. Freezes are never stored - they're just
/// recomputed each time by walking backward and counting misses per month,
/// so there's nothing to reset explicitly; a new month simply has a fresh 5.
///
/// Freezes only ever bridge a gap *between* real completions - they never
/// extend backward past the earliest completion that actually exists. Without
/// that floor, someone with a single completion ever would incorrectly show
/// a multi-day streak, since the walk-back would keep "forgiving" empty days
/// indefinitely with nothing real behind them.
/// </summary>
public static class StreakCalculator
{
    private const int FreeRestoresPerMonth = 5;

    public static int Compute(IReadOnlySet<DateOnly> completionDates, DateOnly today)
    {
        if (completionDates.Count == 0) return 0;

        var earliest = completionDates.Min();
        var freezesUsed = new Dictionary<(int Year, int Month), int>();

        var cursor = completionDates.Contains(today) ? today : today.AddDays(-1);
        if (cursor < earliest || !IsDaySafe(cursor, completionDates, freezesUsed)) return 0;

        var streak = 0;
        while (cursor >= earliest && IsDaySafe(cursor, completionDates, freezesUsed))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    /// <summary>
    /// Shared streak: a day only counts if BOTH users had a safe day (completed
    /// or covered by their own monthly freeze allowance - each person's freezes
    /// are independent, so one person running out doesn't affect the other's quota).
    /// Floored at whichever of the two started completing Wird later - a shared
    /// streak can't reach back before both of them actually had a first day.
    /// </summary>
    public static int ComputeShared(IReadOnlySet<DateOnly> datesA, IReadOnlySet<DateOnly> datesB, DateOnly today)
    {
        if (datesA.Count == 0 || datesB.Count == 0) return 0;

        var earliest = new[] { datesA.Min(), datesB.Min() }.Max();
        var freezesA = new Dictionary<(int Year, int Month), int>();
        var freezesB = new Dictionary<(int Year, int Month), int>();

        var todayBothDone = datesA.Contains(today) && datesB.Contains(today);
        var cursor = todayBothDone ? today : today.AddDays(-1);

        if (cursor < earliest || !(IsDaySafe(cursor, datesA, freezesA) && IsDaySafe(cursor, datesB, freezesB)))
        {
            return 0;
        }

        var streak = 0;
        while (cursor >= earliest && IsDaySafe(cursor, datesA, freezesA) && IsDaySafe(cursor, datesB, freezesB))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }

    private static bool IsDaySafe(DateOnly date, IReadOnlySet<DateOnly> completions, Dictionary<(int, int), int> freezesUsedByMonth)
    {
        if (completions.Contains(date)) return true;

        var monthKey = (date.Year, date.Month);
        freezesUsedByMonth.TryGetValue(monthKey, out var used);

        if (used >= FreeRestoresPerMonth) return false;

        freezesUsedByMonth[monthKey] = used + 1;
        return true;
    }
}

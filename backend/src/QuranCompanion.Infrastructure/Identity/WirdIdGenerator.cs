using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Identity;

/// <summary>
/// Generates ids like WIRD-8K4X29: unguessable enough to share safely,
/// short enough to read aloud or type by hand.
/// </summary>
public class WirdIdGenerator : IWirdIdGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I to avoid confusion
    private const int Length = 6;
    private readonly AppDbContext _db;

    public WirdIdGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateUniqueAsync(CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"WIRD-{RandomSuffix()}";
            var exists = await _db.Users.AnyAsync(u => u.WirdId == candidate, ct);
            if (!exists) return candidate;
        }

        throw new InvalidOperationException("Could not generate a unique Wird ID after several attempts.");
    }

    private static string RandomSuffix()
    {
        Span<char> buffer = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }
        return new string(buffer);
    }
}

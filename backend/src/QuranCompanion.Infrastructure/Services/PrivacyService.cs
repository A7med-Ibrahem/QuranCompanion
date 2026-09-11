using Microsoft.EntityFrameworkCore;
using QuranCompanion.Application.Common.Interfaces;
using QuranCompanion.Application.DTOs.Companions;
using QuranCompanion.Domain.Entities;
using QuranCompanion.Infrastructure.Persistence;

namespace QuranCompanion.Infrastructure.Services;

public class PrivacyService : IPrivacyService
{
    private readonly AppDbContext _db;

    public PrivacyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PrivacySettingsDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var settings = await _db.PrivacySettings.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        // No row yet = defaults (completion status only) - the privacy-first behavior spec section 6 asks for.
        return settings is null
            ? new PrivacySettingsDto(true, false, false, false)
            : ToDto(settings);
    }

    public async Task<PrivacySettingsDto> UpdateAsync(Guid userId, UpdatePrivacySettingsRequest request, CancellationToken ct = default)
    {
        var settings = await _db.PrivacySettings.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (settings is null)
        {
            settings = new PrivacySettings { UserId = userId };
            _db.PrivacySettings.Add(settings);
        }

        settings.ShareCompletionStatus = request.ShareCompletionStatus;
        settings.ShareStreak = request.ShareStreak;
        settings.ShareWirdRange = request.ShareWirdRange;
        settings.ShareReadingProgress = request.ShareReadingProgress;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(settings);
    }

    private static PrivacySettingsDto ToDto(PrivacySettings s) =>
        new(s.ShareCompletionStatus, s.ShareStreak, s.ShareWirdRange, s.ShareReadingProgress);
}

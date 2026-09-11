using QuranCompanion.Application.DTOs.Companions;

namespace QuranCompanion.Application.Common.Interfaces;

public interface IPrivacyService
{
    Task<PrivacySettingsDto> GetAsync(Guid userId, CancellationToken ct = default);
    Task<PrivacySettingsDto> UpdateAsync(Guid userId, UpdatePrivacySettingsRequest request, CancellationToken ct = default);
}

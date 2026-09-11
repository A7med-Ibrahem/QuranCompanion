namespace QuranCompanion.Application.Common.Interfaces;

public interface IWirdIdGenerator
{
    /// <summary>Generates a unique, shareable, non-guessable identifier, e.g. WIRD-8K4X29.</summary>
    Task<string> GenerateUniqueAsync(CancellationToken ct = default);
}

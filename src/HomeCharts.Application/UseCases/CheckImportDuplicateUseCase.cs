using System.Security.Cryptography;
using System.Text;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class CheckImportDuplicateUseCase(IImportBatchRepository importBatchRepository)
{
    private readonly IImportBatchRepository _importBatchRepository = importBatchRepository;

    public async Task<ImportDuplicateResult> ExecuteAsync(string sourceName, string fileContent, CancellationToken cancellationToken = default)
    {
        var normalizedSource = sourceName?.Trim() ?? string.Empty;
        var hash = ComputeSha256Hex(fileContent ?? string.Empty);
        var existing = await _importBatchRepository.GetByFileHashAsync(hash, cancellationToken);

        return new ImportDuplicateResult(
            normalizedSource,
            hash,
            existing is not null,
            existing?.Id,
            existing?.ImportedAtUtc);
    }

    private static string ComputeSha256Hex(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value.Replace("\r\n", "\n", StringComparison.Ordinal));
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

public sealed record ImportDuplicateResult(
    string SourceName,
    string FileHash,
    bool IsDuplicate,
    Guid? ExistingBatchId,
    DateTimeOffset? ExistingImportedAtUtc);

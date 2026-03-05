using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class ImportBankStatementUseCase(
    ITransactionRepository transactionRepository,
    IImportBatchRepository importBatchRepository,
    BankStatementParser parser,
    CheckImportDuplicateUseCase checkImportDuplicateUseCase)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IImportBatchRepository _importBatchRepository = importBatchRepository;
    private readonly BankStatementParser _parser = parser;
    private readonly CheckImportDuplicateUseCase _checkImportDuplicateUseCase = checkImportDuplicateUseCase;

    public async Task<ImportBankStatementResult> ExecuteAsync(
        string sourceName,
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        var duplicateCheck = await _checkImportDuplicateUseCase.ExecuteAsync(sourceName, fileContent, cancellationToken);
        if (duplicateCheck.IsDuplicate)
        {
            return new ImportBankStatementResult(
                duplicateCheck.SourceName,
                duplicateCheck.FileHash,
                true,
                0,
                [new BankStatementParseError(0, "Duplicate import file hash detected.", string.Empty)],
                [new BankStatementParseWarning(0, BankStatementWarningCode.DuplicateFileHash, "Import skipped because file hash is duplicate.")],
                BankStatementParseMetrics.Empty,
                0,
                []);
        }

        var parse = _parser.Parse(fileContent);
        if (parse.Errors.Count > 0)
        {
            return new ImportBankStatementResult(
                duplicateCheck.SourceName,
                duplicateCheck.FileHash,
                false,
                0,
                parse.Errors,
                parse.Warnings,
                parse.Metrics,
                0,
                []);
        }

        var dedup = DeduplicateRows(parse.Rows);
        var existingFingerprints = await _transactionRepository.GetExistingFingerprintsAsync(
            dedup.Rows.Select(CreateRowFingerprint).ToArray(),
            cancellationToken);
        var crossImport = RemoveRowsAlreadyPersisted(dedup.Rows, existingFingerprints);
        var warnings = parse.Warnings
            .Concat(dedup.Warnings)
            .Concat(crossImport.Warnings)
            .ToArray();

        var now = DateTimeOffset.UtcNow;
        var transactions = crossImport.Rows.Select(row => new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = row.BookingDate,
            ValueDate = row.ValueDate,
            Description = row.Description,
            NormalizedDescription = DescriptionNormalizer.Normalize(row.Description),
            TransactionFingerprint = CreateRowFingerprint(row),
            Amount = row.Amount,
            Currency = "EUR",
            SourceAccount = row.SourceAccount,
            ExternalReference = row.ExternalReference,
            CreatedUtc = now,
            UpdatedUtc = now
        }).ToArray();

        await _transactionRepository.UpsertManyAsync(transactions, cancellationToken);

        await _importBatchRepository.UpsertAsync(new ImportBatch
        {
            Id = Guid.NewGuid(),
            SourceName = duplicateCheck.SourceName,
            FileHash = duplicateCheck.FileHash,
            ImportedAtUtc = now,
            ImportedCount = transactions.Length,
            SkippedCount = dedup.SkippedDuplicateRowCount + crossImport.SkippedExistingDuplicateRowCount
        }, cancellationToken);

        var metrics = parse.Metrics with
        {
            WarningCount = warnings.Length,
            DuplicateRowCount = dedup.SkippedDuplicateRowCount,
            ImportedRowCount = transactions.Length,
            DuplicateExistingRowCount = crossImport.SkippedExistingDuplicateRowCount
        };

        return new ImportBankStatementResult(
            duplicateCheck.SourceName,
            duplicateCheck.FileHash,
            false,
            transactions.Length,
            [],
            warnings,
            metrics,
            dedup.SkippedDuplicateRowCount + crossImport.SkippedExistingDuplicateRowCount,
            transactions.Select(static transaction => transaction.Id).ToArray());
    }

    private static DeduplicationResult DeduplicateRows(IReadOnlyList<BankStatementRow> rows)
    {
        var distinctRows = new List<BankStatementRow>(rows.Count);
        var warnings = new List<BankStatementParseWarning>();
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var fingerprint = CreateRowFingerprint(row);
            if (!fingerprints.Add(fingerprint))
            {
                warnings.Add(new BankStatementParseWarning(
                    row.LineNumber,
                    BankStatementWarningCode.DuplicateRowInFile,
                    "Duplicate row detected in the same import file and skipped."));
                continue;
            }

            distinctRows.Add(row);
        }

        return new DeduplicationResult(distinctRows, warnings, warnings.Count);
    }

    private static string CreateRowFingerprint(BankStatementRow row)
        => TransactionFingerprintBuilder.Build(
            row.BookingDate,
            row.ValueDate,
            row.Description,
            row.Amount,
            row.RunningBalance,
            row.ExternalReference,
            row.SourceAccount);

    private static CrossImportDeduplicationResult RemoveRowsAlreadyPersisted(
        IReadOnlyList<BankStatementRow> rows,
        IReadOnlySet<string> existingFingerprints)
    {
        if (rows.Count == 0 || existingFingerprints.Count == 0)
        {
            return new CrossImportDeduplicationResult(rows, [], 0);
        }

        var remainingRows = new List<BankStatementRow>(rows.Count);
        var warnings = new List<BankStatementParseWarning>();

        foreach (var row in rows)
        {
            var fingerprint = CreateRowFingerprint(row);
            if (existingFingerprints.Contains(fingerprint))
            {
                warnings.Add(new BankStatementParseWarning(
                    row.LineNumber,
                    BankStatementWarningCode.DuplicateRowInDatabase,
                    "Row already exists in persisted transactions and was skipped."));
                continue;
            }

            remainingRows.Add(row);
        }

        return new CrossImportDeduplicationResult(remainingRows, warnings, warnings.Count);
    }

    private sealed record DeduplicationResult(
        IReadOnlyList<BankStatementRow> Rows,
        IReadOnlyList<BankStatementParseWarning> Warnings,
        int SkippedDuplicateRowCount);

    private sealed record CrossImportDeduplicationResult(
        IReadOnlyList<BankStatementRow> Rows,
        IReadOnlyList<BankStatementParseWarning> Warnings,
        int SkippedExistingDuplicateRowCount);
}

public sealed record ImportBankStatementResult(
    string SourceName,
    string FileHash,
    bool IsDuplicate,
    int ImportedCount,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings,
    BankStatementParseMetrics Metrics,
    int SkippedDuplicateRowCount,
    IReadOnlyList<Guid> ImportedTransactionIds);

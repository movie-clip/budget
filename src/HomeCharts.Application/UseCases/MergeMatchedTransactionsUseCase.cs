using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class MergeMatchedTransactionsUseCase(
    ITransactionRepository transactionRepository,
    IImportBatchRepository importBatchRepository,
    IRuleRepository ruleRepository,
    BankStatementParser parser,
    CheckImportDuplicateUseCase checkImportDuplicateUseCase)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IImportBatchRepository _importBatchRepository = importBatchRepository;
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly BankStatementParser _parser = parser;
    private readonly CheckImportDuplicateUseCase _checkImportDuplicateUseCase = checkImportDuplicateUseCase;

    public async Task<MergeMatchedTransactionsResult> ExecuteAsync(
        string sourceName,
        string fileContent,
        CancellationToken cancellationToken = default)
    {
        var duplicateCheck = await _checkImportDuplicateUseCase.ExecuteAsync(sourceName, fileContent, cancellationToken);
        if (duplicateCheck.IsDuplicate)
        {
            return new MergeMatchedTransactionsResult(
                duplicateCheck.SourceName,
                duplicateCheck.FileHash,
                true,
                0,
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
            return new MergeMatchedTransactionsResult(
                duplicateCheck.SourceName,
                duplicateCheck.FileHash,
                false,
                0,
                0,
                parse.Errors,
                parse.Warnings,
                parse.Metrics,
                0,
                []);
        }

        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var matchedRows = new List<MatchedRow>(parse.Rows.Count);
        var unmatchedCount = 0;

        foreach (var row in parse.Rows)
        {
            var normalized = DescriptionNormalizer.Normalize(row.Description);
            var match = CategorizationEngine.Match(normalized, rules);
            if (match is null)
            {
                unmatchedCount++;
                continue;
            }

            matchedRows.Add(new MatchedRow(row, match.CategoryId));
        }

        var dedup = DeduplicateRows(matchedRows);
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
            BookingDate = row.Row.BookingDate,
            ValueDate = row.Row.ValueDate,
            Description = row.Row.Description,
            NormalizedDescription = DescriptionNormalizer.Normalize(row.Row.Description),
            TransactionFingerprint = CreateRowFingerprint(row),
            Amount = row.Row.Amount,
            Currency = "EUR",
            SourceAccount = row.Row.SourceAccount,
            ExternalReference = row.Row.ExternalReference,
            CategoryId = row.CategoryId,
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
            SkippedCount = dedup.SkippedDuplicateRowCount + crossImport.SkippedExistingDuplicateRowCount + unmatchedCount
        }, cancellationToken);

        var metrics = parse.Metrics with
        {
            WarningCount = warnings.Length,
            DuplicateRowCount = dedup.SkippedDuplicateRowCount,
            ImportedRowCount = transactions.Length,
            DuplicateExistingRowCount = crossImport.SkippedExistingDuplicateRowCount
        };

        return new MergeMatchedTransactionsResult(
            duplicateCheck.SourceName,
            duplicateCheck.FileHash,
            false,
            matchedRows.Count,
            unmatchedCount,
            [],
            warnings,
            metrics,
            dedup.SkippedDuplicateRowCount + crossImport.SkippedExistingDuplicateRowCount,
            transactions.Select(static transaction => transaction.Id).ToArray());
    }

    private static DeduplicationResult DeduplicateRows(IReadOnlyList<MatchedRow> rows)
    {
        var distinctRows = new List<MatchedRow>(rows.Count);
        var warnings = new List<BankStatementParseWarning>();
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var fingerprint = CreateRowFingerprint(row);
            if (!fingerprints.Add(fingerprint))
            {
                warnings.Add(new BankStatementParseWarning(
                    row.Row.LineNumber,
                    BankStatementWarningCode.DuplicateRowInFile,
                    "Duplicate matched row detected in the same import file and skipped."));
                continue;
            }

            distinctRows.Add(row);
        }

        return new DeduplicationResult(distinctRows, warnings, warnings.Count);
    }

    private static string CreateRowFingerprint(MatchedRow row)
        => TransactionFingerprintBuilder.Build(
            row.Row.BookingDate,
            row.Row.ValueDate,
            row.Row.Description,
            row.Row.Amount,
            row.Row.RunningBalance,
            row.Row.ExternalReference,
            row.Row.SourceAccount);

    private static CrossImportDeduplicationResult RemoveRowsAlreadyPersisted(
        IReadOnlyList<MatchedRow> rows,
        IReadOnlySet<string> existingFingerprints)
    {
        if (rows.Count == 0 || existingFingerprints.Count == 0)
        {
            return new CrossImportDeduplicationResult(rows, [], 0);
        }

        var remainingRows = new List<MatchedRow>(rows.Count);
        var warnings = new List<BankStatementParseWarning>();

        foreach (var row in rows)
        {
            var fingerprint = CreateRowFingerprint(row);
            if (existingFingerprints.Contains(fingerprint))
            {
                warnings.Add(new BankStatementParseWarning(
                    row.Row.LineNumber,
                    BankStatementWarningCode.DuplicateRowInDatabase,
                    "Matched row already exists in persisted transactions and was skipped."));
                continue;
            }

            remainingRows.Add(row);
        }

        return new CrossImportDeduplicationResult(remainingRows, warnings, warnings.Count);
    }

    private sealed record MatchedRow(BankStatementRow Row, Guid CategoryId);

    private sealed record DeduplicationResult(
        IReadOnlyList<MatchedRow> Rows,
        IReadOnlyList<BankStatementParseWarning> Warnings,
        int SkippedDuplicateRowCount);

    private sealed record CrossImportDeduplicationResult(
        IReadOnlyList<MatchedRow> Rows,
        IReadOnlyList<BankStatementParseWarning> Warnings,
        int SkippedExistingDuplicateRowCount);
}

public sealed record MergeMatchedTransactionsResult(
    string SourceName,
    string FileHash,
    bool IsDuplicate,
    int MatchedCount,
    int UnmatchedCount,
    IReadOnlyList<BankStatementParseError> Errors,
    IReadOnlyList<BankStatementParseWarning> Warnings,
    BankStatementParseMetrics Metrics,
    int SkippedDuplicateRowCount,
    IReadOnlyList<Guid> ImportedTransactionIds);

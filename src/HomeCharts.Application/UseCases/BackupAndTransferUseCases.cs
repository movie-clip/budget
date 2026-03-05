using System.Text.Json;
using HomeCharts.Contracts.Operations;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.UseCases;

public sealed class CreateDatabaseBackupUseCase(IDataMaintenanceService dataMaintenanceService)
{
    private readonly IDataMaintenanceService _dataMaintenanceService = dataMaintenanceService;

    public async Task<BackupResult> ExecuteAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        await _dataMaintenanceService.BackupDatabaseAsync(targetFilePath, cancellationToken);
        return new BackupResult(_dataMaintenanceService.GetDatabasePath(), targetFilePath, DateTimeOffset.UtcNow);
    }
}

public sealed class RestoreDatabaseBackupUseCase(IDataMaintenanceService dataMaintenanceService)
{
    private readonly IDataMaintenanceService _dataMaintenanceService = dataMaintenanceService;

    public async Task<RestoreResult> ExecuteAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        await _dataMaintenanceService.RestoreDatabaseAsync(sourceFilePath, cancellationToken);
        return new RestoreResult(sourceFilePath, _dataMaintenanceService.GetDatabasePath(), DateTimeOffset.UtcNow);
    }
}

public sealed class ExportDataPackageUseCase(
    ICategoryRepository categoryRepository,
    IRuleRepository ruleRepository,
    ITransactionRepository transactionRepository)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task<ExportResult> ExecuteAsync(string targetFilePath, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var rules = await _ruleRepository.GetActiveAsync(cancellationToken);
        var transactions = await _transactionRepository.GetByDateRangeAsync(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31), cancellationToken);

        var package = new DataPackage(
            SchemaVersion: 1,
            ExportedAtUtc: DateTimeOffset.UtcNow,
            Categories: categories,
            Rules: rules,
            Transactions: transactions);

        var json = JsonSerializer.Serialize(package, SerializerOptions);
        var folder = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await File.WriteAllTextAsync(targetFilePath, json, cancellationToken);
        return new ExportResult(targetFilePath, categories.Count, rules.Count, transactions.Count);
    }
}

public sealed class ImportDataPackageUseCase(
    ICategoryRepository categoryRepository,
    IRuleRepository ruleRepository,
    ITransactionRepository transactionRepository)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IRuleRepository _ruleRepository = ruleRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    public async Task<ImportResult> ExecuteAsync(string sourceFilePath, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(sourceFilePath, cancellationToken);
        var package = JsonSerializer.Deserialize<DataPackage>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Invalid data package content.");

        if (package.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported package schema version: {package.SchemaVersion}.");
        }

        foreach (var category in package.Categories)
        {
            await _categoryRepository.UpsertAsync(category, cancellationToken);
        }

        foreach (var rule in package.Rules)
        {
            await _ruleRepository.UpsertAsync(rule, cancellationToken);
        }

        await _transactionRepository.UpsertManyAsync(package.Transactions, cancellationToken);

        return new ImportResult(package.Categories.Count, package.Rules.Count, package.Transactions.Count);
    }
}

public sealed record DataPackage(
    int SchemaVersion,
    DateTimeOffset ExportedAtUtc,
    IReadOnlyList<Category> Categories,
    IReadOnlyList<CategorizationRule> Rules,
    IReadOnlyList<Transaction> Transactions);

public sealed record BackupResult(string SourceDatabasePath, string BackupFilePath, DateTimeOffset CreatedAtUtc);
public sealed record RestoreResult(string SourceBackupPath, string RestoredDatabasePath, DateTimeOffset RestoredAtUtc);
public sealed record ExportResult(string FilePath, int CategoryCount, int RuleCount, int TransactionCount);
public sealed record ImportResult(int ImportedCategories, int ImportedRules, int ImportedTransactions);

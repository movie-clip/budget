using HomeCharts.Contracts.Operations;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.Tests;

/// <summary>
/// Shared in-memory repository/service fakes for wiring use cases (and, transitively,
/// <c>MainWindowViewModel</c>) without a real SQLite database. Extend this file rather
/// than re-declaring a fake locally — see <c>testing.md</c> "Reuse the shared scaffolding".
/// </summary>
internal sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _items = [];

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Category>>(_items.ToList());

    public Task UpsertAsync(Category category, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.Id == category.Id);
        if (index >= 0)
        {
            _items[index] = category;
        }
        else
        {
            _items.Add(category);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(item => item.Id == categoryId);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryRuleRepository : IRuleRepository
{
    private readonly List<CategorizationRule> _items = [];

    public Task<IReadOnlyList<CategorizationRule>> GetActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CategorizationRule>>(_items.Where(static rule => rule.IsActive).ToList());

    public Task UpsertAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.Id == rule.Id);
        if (index >= 0)
        {
            _items[index] = rule;
        }
        else
        {
            _items.Add(rule);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(item => item.Id == ruleId);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _items = [];

    public Task UpsertManyAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        foreach (var transaction in transactions)
        {
            var index = _items.FindIndex(item => item.Id == transaction.Id);
            if (index >= 0)
            {
                _items[index] = transaction;
            }
            else
            {
                _items.Add(transaction);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateOnly fromInclusive, DateOnly toInclusive, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Transaction>>(_items
            .Where(item => !item.IsDeleted && item.BookingDate >= fromInclusive && item.BookingDate <= toInclusive)
            .ToList());

    public Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(item => item.Id == transactionId));

    public Task SetCategoryAsync(Guid transactionId, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.Id == transactionId);
        if (index >= 0)
        {
            _items[index] = _items[index] with { CategoryId = categoryId };
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlySet<string>> GetExistingFingerprintsAsync(IReadOnlyCollection<string> fingerprints, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlySet<string>>(_items.Select(item => item.TransactionFingerprint).ToHashSet(StringComparer.Ordinal));

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var deleted = _items.Count;
        _items.Clear();
        return Task.FromResult(deleted);
    }
}

internal sealed class InMemoryPrefixFilterRepository : IPrefixFilterRepository
{
    private readonly List<string> _items = [];

    public Task<IReadOnlyList<string>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(_items.ToList());

    public Task AddAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (!_items.Contains(prefix, StringComparer.Ordinal))
        {
            _items.Add(prefix);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string prefix, CancellationToken cancellationToken = default)
    {
        _items.RemoveAll(item => string.Equals(item, prefix, StringComparison.Ordinal));
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryImportBatchRepository : IImportBatchRepository
{
    private readonly List<ImportBatch> _items = [];

    public Task<ImportBatch?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.SingleOrDefault(item => string.Equals(item.FileHash, fileHash, StringComparison.Ordinal)));

    public Task UpsertAsync(ImportBatch batch, CancellationToken cancellationToken = default)
    {
        var index = _items.FindIndex(item => item.Id == batch.Id);
        if (index >= 0)
        {
            _items[index] = batch;
        }
        else
        {
            _items.Add(batch);
        }

        return Task.CompletedTask;
    }

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var deleted = _items.Count;
        _items.Clear();
        return Task.FromResult(deleted);
    }
}

internal sealed class InMemoryManualOverrideRepository : IManualOverrideRepository
{
    private readonly List<ManualOverride> _items = [];

    public Task AddAsync(ManualOverride manualOverride, CancellationToken cancellationToken = default)
    {
        _items.Add(manualOverride);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ManualOverride>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ManualOverride>>(_items.Where(item => item.TransactionId == transactionId).ToList());

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var deleted = _items.Count;
        _items.Clear();
        return Task.FromResult(deleted);
    }
}

internal sealed class NoOpMigrationRunner : IMigrationRunner
{
    public Task EnsureCreatedAndMigratedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class NoOpDataMaintenanceService(string databasePath = "C:/data/homecharts.db") : IDataMaintenanceService
{
    public Task BackupDatabaseAsync(string targetFilePath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RestoreDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public string GetDatabasePath() => databasePath;
}

/// <summary>The full set of in-memory repositories a <c>MainWindowViewModel</c> needs.</summary>
internal sealed class TestRepositories
{
    public InMemoryTransactionRepository Transactions { get; } = new();
    public InMemoryCategoryRepository Categories { get; } = new();
    public InMemoryRuleRepository Rules { get; } = new();
    public InMemoryPrefixFilterRepository PrefixFilters { get; } = new();
    public InMemoryImportBatchRepository ImportBatches { get; } = new();
    public InMemoryManualOverrideRepository ManualOverrides { get; } = new();
}

/// <summary>
/// Single home for the ~22-argument <c>MainWindowViewModel</c> constructor call so a signature
/// change is fixed in one place instead of once per view-model test file.
/// </summary>
internal static class TestViewModelFactory
{
    public static HomeCharts.Presentation.Mvp.MainWindowViewModel Create(TestRepositories repositories)
    {
        var parser = new HomeCharts.Application.Import.BankStatementParser();
        var checkImportDuplicateUseCase = new HomeCharts.Application.UseCases.CheckImportDuplicateUseCase(repositories.ImportBatches);
        var createRule = new HomeCharts.Application.UseCases.CreateCategorizationRuleUseCase(repositories.Rules);

        return new HomeCharts.Presentation.Mvp.MainWindowViewModel(
            new HomeCharts.Application.UseCases.InitializeDatabaseUseCase(new NoOpMigrationRunner()),
            new HomeCharts.Application.UseCases.SeedDefaultCategoriesUseCase(repositories.Categories),
            new HomeCharts.Application.UseCases.ImportBankStatementUseCase(repositories.Transactions, repositories.ImportBatches, parser, checkImportDuplicateUseCase),
            new HomeCharts.Application.UseCases.PreviewMatchedTransactionsUseCase(repositories.Rules, repositories.Categories, parser),
            new HomeCharts.Application.UseCases.MergeMatchedTransactionsUseCase(repositories.Transactions, repositories.ImportBatches, repositories.Rules, parser, checkImportDuplicateUseCase),
            new HomeCharts.Application.UseCases.ApplyCategorizationRulesUseCase(repositories.Transactions, repositories.Rules, repositories.ManualOverrides, repositories.Categories),
            new HomeCharts.Application.UseCases.PreviewPotentialRulesUseCase(repositories.Rules, repositories.Categories, repositories.PrefixFilters, parser),
            new HomeCharts.Application.UseCases.PreviewParsedCategoryExpensesUseCase(repositories.Rules, repositories.Categories, parser),
            new HomeCharts.Application.UseCases.DeleteAllRulesUseCase(repositories.Rules),
            new HomeCharts.Application.UseCases.DeleteAllTransactionsUseCase(repositories.ManualOverrides, repositories.Transactions, repositories.ImportBatches),
            new HomeCharts.Application.UseCases.ApplyPotentialRulesUseCase(createRule),
            new HomeCharts.Application.UseCases.GetLedgerEntriesUseCase(repositories.Transactions, repositories.Categories),
            new HomeCharts.Application.UseCases.BuildDashboardSnapshotUseCase(repositories.Transactions, repositories.Categories),
            new HomeCharts.Application.UseCases.GetCategoriesUseCase(repositories.Categories),
            new HomeCharts.Application.UseCases.ManualRecategorizationUseCase(repositories.Transactions, repositories.ManualOverrides),
            new HomeCharts.Application.UseCases.GetPrefixFiltersUseCase(repositories.PrefixFilters),
            new HomeCharts.Application.UseCases.AddPrefixFilterUseCase(repositories.PrefixFilters),
            new HomeCharts.Application.UseCases.RemovePrefixFilterUseCase(repositories.PrefixFilters),
            new HomeCharts.Application.UseCases.CreateDatabaseBackupUseCase(new NoOpDataMaintenanceService()),
            new HomeCharts.Application.UseCases.RestoreDatabaseBackupUseCase(new NoOpDataMaintenanceService()),
            new HomeCharts.Application.UseCases.ExportDataPackageUseCase(repositories.Categories, repositories.Rules, repositories.Transactions),
            new HomeCharts.Application.UseCases.ImportDataPackageUseCase(repositories.Categories, repositories.Rules, repositories.Transactions));
    }
}

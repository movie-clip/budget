using HomeCharts.Application.UseCases;
using HomeCharts.Contracts.Operations;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.Tests;

[TestClass]
public sealed class Phase5OperationsTests
{
    [TestMethod]
    public async Task ExportAndImportDataPackage_RoundTripsCoreData()
    {
        var categoryRepository = new InMemoryCategoryRepository();
        var ruleRepository = new InMemoryRuleRepository();
        var transactionRepository = new InMemoryTransactionRepository();

        var category = new Category { Id = Guid.NewGuid(), Name = "Groceries", ColorHex = "#22C55E", IsSystem = true };
        await categoryRepository.UpsertAsync(category);

        var rule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            Name = "Contains supermarket",
            Pattern = "SUPERMARKET",
            MatchType = RuleMatchType.Contains,
            Priority = 10,
            CategoryId = category.Id,
            IsActive = true
        };
        await ruleRepository.UpsertAsync(rule);

        await transactionRepository.UpsertManyAsync([
            new Transaction
            {
                Id = Guid.NewGuid(),
                BookingDate = new DateOnly(2026, 2, 1),
                Description = "SUPERMARKET MERCADONA",
                NormalizedDescription = "SUPERMARKET MERCADONA",
                Amount = -54.22m,
                CategoryId = category.Id,
                TransactionFingerprint = "fp-1"
            }
        ]);

        var filePath = Path.Combine(Path.GetTempPath(), "homecharts-migration-tests", Guid.NewGuid().ToString("N"), "package.json");

        var exportUseCase = new ExportDataPackageUseCase(categoryRepository, ruleRepository, transactionRepository);
        var exportResult = await exportUseCase.ExecuteAsync(filePath);
        Assert.AreEqual(1, exportResult.CategoryCount);
        Assert.AreEqual(1, exportResult.RuleCount);
        Assert.AreEqual(1, exportResult.TransactionCount);

        var importCategoryRepository = new InMemoryCategoryRepository();
        var importRuleRepository = new InMemoryRuleRepository();
        var importTransactionRepository = new InMemoryTransactionRepository();

        var importUseCase = new ImportDataPackageUseCase(importCategoryRepository, importRuleRepository, importTransactionRepository);
        var importResult = await importUseCase.ExecuteAsync(filePath);

        Assert.AreEqual(1, importResult.ImportedCategories);
        Assert.AreEqual(1, importResult.ImportedRules);
        Assert.AreEqual(1, importResult.ImportedTransactions);

        var importedCategories = await importCategoryRepository.GetAllAsync();
        var importedRules = await importRuleRepository.GetActiveAsync();
        var importedTransactions = await importTransactionRepository.GetByDateRangeAsync(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

        Assert.AreEqual("Groceries", importedCategories.Single().Name);
        Assert.AreEqual("Contains supermarket", importedRules.Single().Name);
        Assert.AreEqual("SUPERMARKET MERCADONA", importedTransactions.Single().Description);
    }

    [TestMethod]
    public async Task BackupAndRestoreUseCases_InvokeMaintenanceService()
    {
        var maintenance = new FakeDataMaintenanceService("C:/data/homecharts.db");
        var backupUseCase = new CreateDatabaseBackupUseCase(maintenance);
        var restoreUseCase = new RestoreDatabaseBackupUseCase(maintenance);

        var backupResult = await backupUseCase.ExecuteAsync("C:/backup/homecharts.db");
        var restoreResult = await restoreUseCase.ExecuteAsync("C:/backup/homecharts.db");

        Assert.AreEqual("C:/data/homecharts.db", backupResult.SourceDatabasePath);
        Assert.AreEqual("C:/backup/homecharts.db", backupResult.BackupFilePath);
        Assert.AreEqual("C:/backup/homecharts.db", restoreResult.SourceBackupPath);
        Assert.AreEqual("C:/data/homecharts.db", restoreResult.RestoredDatabasePath);
        Assert.AreEqual(1, maintenance.BackupCalls);
        Assert.AreEqual(1, maintenance.RestoreCalls);
    }

    private sealed class FakeDataMaintenanceService(string databasePath) : IDataMaintenanceService
    {
        private readonly string _databasePath = databasePath;

        public int BackupCalls { get; private set; }
        public int RestoreCalls { get; private set; }

        public Task BackupDatabaseAsync(string targetFilePath, CancellationToken cancellationToken = default)
        {
            BackupCalls++;
            return Task.CompletedTask;
        }

        public Task RestoreDatabaseAsync(string sourceFilePath, CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            return Task.CompletedTask;
        }

        public string GetDatabasePath() => _databasePath;
    }

    private sealed class InMemoryCategoryRepository : ICategoryRepository
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

    private sealed class InMemoryRuleRepository : IRuleRepository
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

    private sealed class InMemoryTransactionRepository : ITransactionRepository
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
            => Task.FromResult<IReadOnlyList<Transaction>>(_items.Where(item => item.BookingDate >= fromInclusive && item.BookingDate <= toInclusive).ToList());

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
}

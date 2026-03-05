using HomeCharts.Application.Import;
using HomeCharts.Application.UseCases;
using HomeCharts.Domain.Model;
using HomeCharts.Infrastructure.Persistence.Sqlite;

namespace HomeCharts.Infrastructure.Tests;

[TestClass]
public sealed class WorkflowIntegrationTests
{
    [TestMethod]
    public async Task SqliteBackedWorkflow_MergeMatchedTransactions_FromTwoDifferentFiles_AppendsToBudget()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var transactionRepository = new SqliteTransactionRepository(factory);
            var importBatchRepository = new SqliteImportBatchRepository(factory);
            var categoryRepository = new SqliteCategoryRepository(factory);
            var ruleRepository = new SqliteRuleRepository(factory);

            var seedCategories = new SeedDefaultCategoriesUseCase(categoryRepository);
            await seedCategories.ExecuteAsync();

            var allCategories = await categoryRepository.GetAllAsync();
            var servicesCategory = allCategories.Single(category => string.Equals(category.Name, "Services", StringComparison.OrdinalIgnoreCase));

            var now = DateTimeOffset.UtcNow;
            await ruleRepository.UpsertAsync(new CategorizationRule
            {
                Id = Guid.NewGuid(),
                Name = "OpenAI",
                Pattern = "OPENAI",
                MatchType = RuleMatchType.Contains,
                Priority = 100,
                CategoryId = servicesCategory.Id,
                IsActive = true,
                CreatedUtc = now,
                UpdatedUtc = now
            });

            var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
            var parser = new BankStatementParser();
            var mergeUseCase = new MergeMatchedTransactionsUseCase(
                transactionRepository,
                importBatchRepository,
                ruleRepository,
                parser,
                duplicateCheck);

            const string file1 = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020\n26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";
            const string file2 = "12/07/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI API USAGE-SAN FRANCISCO|12/07/2025|-35.10|21000.00||5402__7020\n11/07/2025|PAGO BIZUM AMIGO CENA|11/07/2025|-18.50|21035.10||";

            var firstMerge = await mergeUseCase.ExecuteAsync("June2025", file1);
            Assert.IsFalse(firstMerge.IsDuplicate);
            Assert.AreEqual(1, firstMerge.MatchedCount);
            Assert.AreEqual(1, firstMerge.Metrics.ImportedRowCount);

            var afterFirstMerge = await transactionRepository.GetByDateRangeAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
            Assert.AreEqual(1, afterFirstMerge.Count);

            var secondMerge = await mergeUseCase.ExecuteAsync("July2025", file2);
            Assert.IsFalse(secondMerge.IsDuplicate);
            Assert.AreEqual(1, secondMerge.MatchedCount);
            Assert.AreEqual(1, secondMerge.Metrics.ImportedRowCount);

            var afterSecondMerge = await transactionRepository.GetByDateRangeAsync(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
            Assert.AreEqual(2, afterSecondMerge.Count);
            Assert.IsTrue(afterSecondMerge.All(transaction => transaction.CategoryId == servicesCategory.Id));
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    [TestMethod]
    public async Task SqliteBackedWorkflow_ImportCategorizeManualOverride_ReapplyRulesPreservesManualChoice()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var transactionRepository = new SqliteTransactionRepository(factory);
            var importBatchRepository = new SqliteImportBatchRepository(factory);
            var categoryRepository = new SqliteCategoryRepository(factory);
            var ruleRepository = new SqliteRuleRepository(factory);
            var manualOverrideRepository = new SqliteManualOverrideRepository(factory);

            var seedCategories = new SeedDefaultCategoriesUseCase(categoryRepository);
            await seedCategories.ExecuteAsync();

            var allCategories = await categoryRepository.GetAllAsync();
            var utilitiesCategory = allCategories.Single(category => string.Equals(category.Name, "Utility", StringComparison.OrdinalIgnoreCase));
            var groceriesCategory = allCategories.Single(category => string.Equals(category.Name, "Grocery", StringComparison.OrdinalIgnoreCase));

            var now = DateTimeOffset.UtcNow;
            await ruleRepository.UpsertAsync(new CategorizationRule
            {
                Id = Guid.NewGuid(),
                Name = "Exact OpenAI",
                Pattern = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                MatchType = RuleMatchType.Exact,
                Priority = 100,
                CategoryId = utilitiesCategory.Id,
                IsActive = true,
                CreatedUtc = now,
                UpdatedUtc = now
            });

            var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
            var parser = new BankStatementParser();
            var importUseCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, duplicateCheck);
            var applyRulesUseCase = new ApplyCategorizationRulesUseCase(transactionRepository, ruleRepository, manualOverrideRepository, categoryRepository);
            var manualRecategorizationUseCase = new ManualRecategorizationUseCase(transactionRepository, manualOverrideRepository);

            const string row = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";
            var importResult = await importUseCase.ExecuteAsync("June2025", row);
            Assert.AreEqual(1, importResult.ImportedCount);

            var applyFirst = await applyRulesUseCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));
            Assert.AreEqual(1, applyFirst.CategorizedCount);

            var transaction = (await transactionRepository.GetByDateRangeAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30))).Single();
            Assert.AreEqual(utilitiesCategory.Id, transaction.CategoryId);

            var manualResult = await manualRecategorizationUseCase.ExecuteAsync([transaction.Id], groceriesCategory.Id, "User corrected category");
            Assert.AreEqual(1, manualResult.ChangedCount);

            var applySecond = await applyRulesUseCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));
            Assert.AreEqual(1, applySecond.SkippedByManualOverrideCount);

            var finalTransaction = await transactionRepository.GetByIdAsync(transaction.Id);
            Assert.IsNotNull(finalTransaction);
            Assert.AreEqual(groceriesCategory.Id, finalTransaction.CategoryId);

            var overrides = await manualOverrideRepository.GetByTransactionIdAsync(transaction.Id);
            Assert.AreEqual(1, overrides.Count);
            Assert.AreEqual("User corrected category", overrides[0].Reason);
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    private static string CreateTempDatabasePath()
    {
        var folder = Path.Combine(Path.GetTempPath(), "homecharts-migration-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "test.db");
    }

    private static void CleanupDatabaseFiles(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, true);
        }
        catch
        {
        }
    }
}

using HomeCharts.Application.Import;
using HomeCharts.Application.UseCases;
using HomeCharts.Domain.Model;
using HomeCharts.Presentation.Mvp;

namespace HomeCharts.Application.Tests;

/// <summary>
/// There is no HomeCharts.Presentation.Tests project (01-recon.md flagged none confirmed
/// to exist; per work order non_goals, this lane does not create one). Application.Tests
/// already project-references HomeCharts.Presentation, so this exercises
/// MainWindowViewModel's trend-bar scaling through its real use cases wired to in-memory
/// repositories, the same seam Phase5OperationsTests already uses for use-case orchestration.
/// </summary>
[TestClass]
public sealed class MainWindowViewModelTrendBarScalingTests
{
    [TestMethod]
    public async Task LoadInitialStateAsync_OneSeriesFarLargerThanTheOther_ScalesBothBarsAgainstOneSharedMaximum()
    {
        var transactionRepository = new InMemoryTransactionRepository();
        var categoryRepository = new InMemoryCategoryRepository();
        var ruleRepository = new InMemoryRuleRepository();
        var prefixFilterRepository = new InMemoryPrefixFilterRepository();
        var importBatchRepository = new InMemoryImportBatchRepository();
        var manualOverrideRepository = new InMemoryManualOverrideRepository();

        var today = DateOnly.FromDateTime(DateTime.Today);
        await transactionRepository.UpsertManyAsync([
            new Transaction
            {
                Id = Guid.NewGuid(),
                BookingDate = today,
                Description = "Salary",
                NormalizedDescription = "SALARY",
                Amount = 100m,
                TransactionFingerprint = "fp-income"
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                BookingDate = today,
                Description = "Rent",
                NormalizedDescription = "RENT",
                Amount = -1000m,
                TransactionFingerprint = "fp-expense"
            }
        ]);

        var parser = new BankStatementParser();
        var checkImportDuplicateUseCase = new CheckImportDuplicateUseCase(importBatchRepository);
        var createCategorizationRuleUseCase = new CreateCategorizationRuleUseCase(ruleRepository);

        var viewModel = new MainWindowViewModel(
            new InitializeDatabaseUseCase(new NoOpMigrationRunner()),
            new SeedDefaultCategoriesUseCase(categoryRepository),
            new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, checkImportDuplicateUseCase),
            new PreviewMatchedTransactionsUseCase(ruleRepository, categoryRepository, parser),
            new MergeMatchedTransactionsUseCase(transactionRepository, importBatchRepository, ruleRepository, parser, checkImportDuplicateUseCase),
            new ApplyCategorizationRulesUseCase(transactionRepository, ruleRepository, manualOverrideRepository, categoryRepository),
            new PreviewPotentialRulesUseCase(ruleRepository, parser),
            new PreviewParsedCategoryExpensesUseCase(ruleRepository, categoryRepository, parser),
            new DeleteAllRulesUseCase(ruleRepository),
            new DeleteAllTransactionsUseCase(manualOverrideRepository, transactionRepository, importBatchRepository),
            createCategorizationRuleUseCase,
            new CreateCategorizationRulesBatchUseCase(createCategorizationRuleUseCase),
            new GetLedgerEntriesUseCase(transactionRepository, categoryRepository),
            new BuildDashboardSnapshotUseCase(transactionRepository, categoryRepository),
            new GetCategoriesUseCase(categoryRepository),
            new ManualRecategorizationUseCase(transactionRepository, manualOverrideRepository),
            new GetPrefixFiltersUseCase(prefixFilterRepository),
            new AddPrefixFilterUseCase(prefixFilterRepository),
            new RemovePrefixFilterUseCase(prefixFilterRepository),
            new CreateDatabaseBackupUseCase(new NoOpDataMaintenanceService()),
            new RestoreDatabaseBackupUseCase(new NoOpDataMaintenanceService()),
            new ExportDataPackageUseCase(categoryRepository, ruleRepository, transactionRepository),
            new ImportDataPackageUseCase(categoryRepository, ruleRepository, transactionRepository));

        await viewModel.LoadInitialStateAsync();

        Assert.IsFalse(viewModel.Status.StartsWith("Error", StringComparison.Ordinal), $"unexpected failure: {viewModel.Status}");

        var currentMonthKey = today.ToString("yyyy-MM");
        var currentMonthBar = viewModel.TrendChartBars.Single(bar => bar.Month == currentMonthKey);

        // Shared maximum across both series is 1000 (the expense amount): expenses should
        // read at the top of the scale and income at a proportionally small fraction of it,
        // not two independently-scaled ~100% bars.
        Assert.AreEqual(100d, currentMonthBar.ExpensesPercent, 0.01d);
        Assert.AreEqual(10d, currentMonthBar.IncomePercent, 0.01d);
        Assert.IsFalse(currentMonthBar.IncomePercent >= 90d, "income and expenses must not both read close to 100% when one series is 10x the other");
    }
}

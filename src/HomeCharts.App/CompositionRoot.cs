using HomeCharts.Application.Import;
using HomeCharts.Application.UseCases;
using HomeCharts.Contracts.Operations;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Infrastructure.Persistence.Sqlite;
using HomeCharts.Presentation.Mvp;

namespace HomeCharts.App;

internal sealed class CompositionRoot
{
    public MainWindowViewModel CreateMainWindowViewModel()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeCharts");
        var dbPath = Path.Combine(appDataFolder, "homecharts-mvp.db");

        var sqliteOptions = new SqliteOptions { DatabasePath = dbPath, EnableWriteAheadLogging = true };
        IAppDbConnectionFactory connectionFactory = new SqliteConnectionFactory(sqliteOptions);
        IDataMaintenanceService dataMaintenanceService = new SqliteDataMaintenanceService(sqliteOptions);

        var migrationRunner = new SqliteMigrationRunner(connectionFactory);
        var categoryRepository = new SqliteCategoryRepository(connectionFactory);
        var ruleRepository = new SqliteRuleRepository(connectionFactory);
        var transactionRepository = new SqliteTransactionRepository(connectionFactory);
        var importBatchRepository = new SqliteImportBatchRepository(connectionFactory);
        var manualOverrideRepository = new SqliteManualOverrideRepository(connectionFactory);
        var prefixFilterRepository = new SqlitePrefixFilterRepository(connectionFactory);

        var initializeDatabaseUseCase = new InitializeDatabaseUseCase(migrationRunner);
        var seedDefaultCategoriesUseCase = new SeedDefaultCategoriesUseCase(categoryRepository);
        var checkImportDuplicateUseCase = new CheckImportDuplicateUseCase(importBatchRepository);
        var bankStatementParser = new BankStatementParser();
        var previewPotentialRulesUseCase = new PreviewPotentialRulesUseCase(ruleRepository, bankStatementParser);
        var previewParsedCategoryExpensesUseCase = new PreviewParsedCategoryExpensesUseCase(ruleRepository, categoryRepository, bankStatementParser);
        var previewMatchedTransactionsUseCase = new PreviewMatchedTransactionsUseCase(ruleRepository, categoryRepository, bankStatementParser);
        var importBankStatementUseCase = new ImportBankStatementUseCase(
            transactionRepository,
            importBatchRepository,
            bankStatementParser,
            checkImportDuplicateUseCase);
        var mergeMatchedTransactionsUseCase = new MergeMatchedTransactionsUseCase(
            transactionRepository,
            importBatchRepository,
            ruleRepository,
            bankStatementParser,
            checkImportDuplicateUseCase);
        var applyCategorizationRulesUseCase = new ApplyCategorizationRulesUseCase(
            transactionRepository,
            ruleRepository,
            manualOverrideRepository,
            categoryRepository);
        var deleteAllRulesUseCase = new DeleteAllRulesUseCase(ruleRepository);
        var deleteAllTransactionsUseCase = new DeleteAllTransactionsUseCase(manualOverrideRepository, transactionRepository, importBatchRepository);
        var createCategorizationRuleUseCase = new CreateCategorizationRuleUseCase(ruleRepository);
        var getLedgerEntriesUseCase = new GetLedgerEntriesUseCase(transactionRepository, categoryRepository);
        var buildDashboardSnapshotUseCase = new BuildDashboardSnapshotUseCase(transactionRepository, categoryRepository);
        var getCategoriesUseCase = new GetCategoriesUseCase(categoryRepository);
        var manualRecategorizationUseCase = new ManualRecategorizationUseCase(transactionRepository, manualOverrideRepository);
        var getPrefixFiltersUseCase = new GetPrefixFiltersUseCase(prefixFilterRepository);
        var addPrefixFilterUseCase = new AddPrefixFilterUseCase(prefixFilterRepository);
        var removePrefixFilterUseCase = new RemovePrefixFilterUseCase(prefixFilterRepository);
        var createDatabaseBackupUseCase = new CreateDatabaseBackupUseCase(dataMaintenanceService);
        var restoreDatabaseBackupUseCase = new RestoreDatabaseBackupUseCase(dataMaintenanceService);
        var exportDataPackageUseCase = new ExportDataPackageUseCase(categoryRepository, ruleRepository, transactionRepository);
        var importDataPackageUseCase = new ImportDataPackageUseCase(categoryRepository, ruleRepository, transactionRepository);

        return new MainWindowViewModel(
            initializeDatabaseUseCase,
            seedDefaultCategoriesUseCase,
            importBankStatementUseCase,
            previewMatchedTransactionsUseCase,
            mergeMatchedTransactionsUseCase,
            applyCategorizationRulesUseCase,
            previewPotentialRulesUseCase,
            previewParsedCategoryExpensesUseCase,
            deleteAllRulesUseCase,
            deleteAllTransactionsUseCase,
            createCategorizationRuleUseCase,
            getLedgerEntriesUseCase,
            buildDashboardSnapshotUseCase,
            getCategoriesUseCase,
            manualRecategorizationUseCase,
            getPrefixFiltersUseCase,
            addPrefixFilterUseCase,
            removePrefixFilterUseCase,
            createDatabaseBackupUseCase,
            restoreDatabaseBackupUseCase,
            exportDataPackageUseCase,
            importDataPackageUseCase);
    }
}

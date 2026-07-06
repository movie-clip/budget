using HomeCharts.Application.UseCases;
using HomeCharts.Application.Import;
using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;
using HomeCharts.Presentation.Mvp;

namespace HomeCharts.Application.Tests;

[TestClass]
public sealed class ApplicationUseCaseTests
{
    [TestMethod]
    public async Task InitializeDatabaseUseCase_CallsMigrationRunner()
    {
        var migrationRunner = new FakeMigrationRunner();
        var useCase = new InitializeDatabaseUseCase(migrationRunner);

        await useCase.ExecuteAsync();

        Assert.AreEqual(1, migrationRunner.CallCount);
    }

    [TestMethod]
    public async Task SeedDefaultCategoriesUseCase_InsertsDefaultsWhenMissing()
    {
        var repository = new FakeCategoryRepository();
        var useCase = new SeedDefaultCategoriesUseCase(repository);

        await useCase.ExecuteAsync();

        Assert.IsTrue(repository.Items.Count >= 10);
        Assert.IsTrue(repository.Items.All(static item => item.IsSystem));
        CollectionAssert.Contains(repository.Items.Select(static item => item.Name).ToList(), "Education");
        CollectionAssert.Contains(repository.Items.Select(static item => item.Name).ToList(), "Grocery");
        CollectionAssert.Contains(repository.Items.Select(static item => item.Name).ToList(), "Travel");
        CollectionAssert.Contains(repository.Items.Select(static item => item.Name).ToList(), "Utility");
    }

    [TestMethod]
    public async Task CreateCategorizationRulesBatchUseCase_CreatesAndCountsCreatedDuplicateAndInvalidItems()
    {
        var groceriesId = Guid.NewGuid();
        var servicesId = Guid.NewGuid();
        var repository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Contains OPENAI",
                    Pattern = "OPENAI",
                    MatchType = RuleMatchType.Contains,
                    Priority = 5,
                    CategoryId = servicesId,
                    IsActive = true
                }
            ]
        };

        var single = new CreateCategorizationRuleUseCase(repository);
        var batch = new CreateCategorizationRulesBatchUseCase(single);

        var result = await batch.ExecuteAsync(
        [
            new CreateCategorizationRulesBatchItem("MERCADONA", groceriesId),
            new CreateCategorizationRulesBatchItem("OPENAI", servicesId),
            new CreateCategorizationRulesBatchItem("   ", groceriesId)
        ]);

        Assert.AreEqual(1, result.CreatedCount);
        Assert.AreEqual(1, result.DuplicateCount);
        Assert.AreEqual(1, result.InvalidCount);
        Assert.IsTrue(repository.Rules.Any(rule => rule.Pattern == "MERCADONA" && rule.CategoryId == groceriesId));
    }

    [TestMethod]
    public void EditableRulesImportedTransactionViewModel_TracksModifiedStateFromDescriptionAndCategory()
    {
        var none = new CategoryOptionViewModel(Guid.NewGuid(), "None", "#94A3B8");
        var education = new CategoryOptionViewModel(Guid.NewGuid(), "Education", "#0EA5E9");
        var row = new EditableRulesImportedTransactionViewModel(
            Guid.NewGuid(),
            new DateOnly(2026, 3, 18),
            "OPENAI *CHATGPT SUBSCR",
            -20.72m,
            none);

        Assert.IsFalse(row.IsModified);

        row.Description = "OPENAI";
        Assert.IsTrue(row.IsModified);

        row.Description = "OPENAI *CHATGPT SUBSCR";
        Assert.IsFalse(row.IsModified);

        row.SelectedCategory = education;
        Assert.IsTrue(row.IsModified);
    }

    [TestMethod]
    public async Task CheckImportDuplicateUseCase_ReturnsDuplicateForExistingHash()
    {
        const string fileContent = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";

        var repository = new FakeImportBatchRepository();
        var duplicateCheck = new CheckImportDuplicateUseCase(repository);

        var first = await duplicateCheck.ExecuteAsync("June2025", fileContent);
        Assert.IsFalse(first.IsDuplicate);

        await repository.UpsertAsync(new ImportBatch
        {
            Id = Guid.NewGuid(),
            SourceName = first.SourceName,
            FileHash = first.FileHash,
            ImportedAtUtc = DateTimeOffset.UtcNow,
            ImportedCount = 42,
            SkippedCount = 0
        });

        var second = await duplicateCheck.ExecuteAsync("June2025", fileContent);
        Assert.IsTrue(second.IsDuplicate);
        Assert.AreEqual(first.FileHash, second.FileHash);
        Assert.IsTrue(second.ExistingBatchId.HasValue);
    }

    [TestMethod]
    public void BankStatementParser_ParsesCopiedBankRecipeRow()
    {
        const string row = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";
        var parser = new BankStatementParser();

        var result = parser.Parse(row);

        Assert.AreEqual(1, result.Rows.Count);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(new DateOnly(2025, 6, 30), result.Rows[0].BookingDate);
        Assert.AreEqual(-20.72m, result.Rows[0].Amount);
        Assert.AreEqual(22482.40m, result.Rows[0].RunningBalance);
        Assert.IsNull(result.Rows[0].ExternalReference);
        Assert.AreEqual("5402__7020", result.Rows[0].SourceAccount);
        Assert.AreEqual(1, result.Metrics.ParsedRowCount);
        Assert.AreEqual(1, result.Metrics.ExpenseCount);
        Assert.AreEqual(0, result.Metrics.WarningCount);
    }

    [TestMethod]
    public void BankStatementParser_ReturnsErrorForInvalidColumnCount()
    {
        const string invalidRow = "30/06/2025|ONLY|THREE";
        var parser = new BankStatementParser();

        var result = parser.Parse(invalidRow);

        Assert.AreEqual(0, result.Rows.Count);
        Assert.AreEqual(1, result.Errors.Count);
        StringAssert.Contains(result.Errors[0].Message, "7 pipe-delimited");
        Assert.AreEqual(1, result.Metrics.ErrorCount);
    }

    [TestMethod]
    public void BankStatementParser_ReportsLineWarningsAndSummaryMetrics()
    {
        const string content = "30/06/2025|SAMPLE|29/06/2025|0|100.00||\n\n26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";
        var parser = new BankStatementParser();

        var result = parser.Parse(content);

        Assert.AreEqual(2, result.Rows.Count);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(2, result.Warnings.Count);
        Assert.IsTrue(result.Warnings.Any(static warning => warning.Code == BankStatementWarningCode.ValueDateBeforeBookingDate));
        Assert.IsTrue(result.Warnings.Any(static warning => warning.Code == BankStatementWarningCode.ZeroAmount));
        Assert.AreEqual(3, result.Metrics.TotalLineCount);
        Assert.AreEqual(1, result.Metrics.EmptyLineCount);
        Assert.AreEqual(2, result.Metrics.NonEmptyLineCount);
        Assert.AreEqual(1, result.Metrics.IncomeCount);
        Assert.AreEqual(0, result.Metrics.ExpenseCount);
        Assert.AreEqual(1, result.Metrics.ZeroAmountCount);
        Assert.AreEqual(0, result.Metrics.DuplicateExistingRowCount);
    }

    [TestMethod]
    public void BankStatementParser_ParsesUtf8BomAndTrimmedColumns()
    {
        const string content = "\uFEFF30/06/2025 | SAMPLE MERCHANT | 30/06/2025 | -20.72 | 22482.40 |  | 5402__7020 ";
        var parser = new BankStatementParser();

        var result = parser.Parse(content);

        Assert.AreEqual(1, result.Rows.Count);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual("SAMPLE MERCHANT", result.Rows[0].Description);
        Assert.AreEqual("5402__7020", result.Rows[0].SourceAccount);
    }

    [TestMethod]
    public void BankStatementParser_ParsesCommaDecimalValues()
    {
        const string content = "30/06/2025|COMISION DIVISA|30/06/2025|-2,88|23363,19||5402057124976021";
        var parser = new BankStatementParser();

        var result = parser.Parse(content);

        Assert.AreEqual(1, result.Rows.Count);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(-2.88m, result.Rows[0].Amount);
        Assert.AreEqual(23363.19m, result.Rows[0].RunningBalance);
    }

    [TestMethod]
    public async Task ImportBankStatementUseCase_PersistsTransactionsAndBatch()
    {
        const string content = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020\n26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";

        var importBatchRepository = new FakeImportBatchRepository();
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var parser = new BankStatementParser();
        var transactionRepository = new FakeTransactionRepository();
        var useCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, duplicateCheck);

        var result = await useCase.ExecuteAsync("June2025", content);

        Assert.IsFalse(result.IsDuplicate);
        Assert.AreEqual(2, result.ImportedCount);
        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(0, result.Warnings.Count);
        Assert.AreEqual(2, transactionRepository.Items.Count);
        Assert.AreEqual(1, importBatchRepository.StoredBatches.Count);
        Assert.AreEqual(2, result.Metrics.ParsedRowCount);
        Assert.AreEqual(1, result.Metrics.IncomeCount);
        Assert.AreEqual(1, result.Metrics.ExpenseCount);
        Assert.AreEqual(2, result.Metrics.ImportedRowCount);
        Assert.AreEqual(0, result.Metrics.DuplicateExistingRowCount);
        Assert.AreEqual(
            "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
            transactionRepository.Items[0].NormalizedDescription);
    }

    [TestMethod]
    public async Task ImportBankStatementUseCase_SkipsDuplicateHash()
    {
        const string content = "20/06/2025|IMPUESTOS Y TASAS RECIBO REFERENCIA CATASTRAL 7160028VH5776S0015SE- MADRID|20/06/2025|-155.03|22862.37||";

        var importBatchRepository = new FakeImportBatchRepository();
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var parser = new BankStatementParser();
        var transactionRepository = new FakeTransactionRepository();
        var useCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, duplicateCheck);

        var first = await useCase.ExecuteAsync("June2025", content);
        var second = await useCase.ExecuteAsync("June2025", content);

        Assert.IsFalse(first.IsDuplicate);
        Assert.IsTrue(second.IsDuplicate);
        Assert.AreEqual(1, transactionRepository.Items.Count);
        Assert.AreEqual(1, importBatchRepository.StoredBatches.Count);
        Assert.AreEqual(1, second.Errors.Count);
        Assert.AreEqual(1, second.Warnings.Count);
        Assert.AreEqual(BankStatementWarningCode.DuplicateFileHash, second.Warnings[0].Code);
        Assert.AreEqual(0, second.Metrics.ParsedRowCount);
        Assert.AreEqual(0, second.SkippedDuplicateRowCount);
    }

    [TestMethod]
    public async Task ImportBankStatementUseCase_SkipsDuplicateRowsWithinFile()
    {
        const string row = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";
        var content = string.Concat(row, "\n", row, "\n26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||");

        var importBatchRepository = new FakeImportBatchRepository();
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var parser = new BankStatementParser();
        var transactionRepository = new FakeTransactionRepository();
        var useCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, duplicateCheck);

        var result = await useCase.ExecuteAsync("June2025", content);

        Assert.IsFalse(result.IsDuplicate);
        Assert.AreEqual(2, result.ImportedCount);
        Assert.AreEqual(1, result.SkippedDuplicateRowCount);
        Assert.AreEqual(3, result.Metrics.ParsedRowCount);
        Assert.AreEqual(2, result.Metrics.ImportedRowCount);
        Assert.AreEqual(1, result.Metrics.DuplicateRowCount);
        Assert.AreEqual(0, result.Metrics.DuplicateExistingRowCount);
        Assert.IsTrue(result.Warnings.Any(static warning => warning.Code == BankStatementWarningCode.DuplicateRowInFile));
        Assert.AreEqual(2, transactionRepository.Items.Count);

        var storedBatch = importBatchRepository.StoredBatches.Single();
        Assert.AreEqual(2, storedBatch.ImportedCount);
        Assert.AreEqual(1, storedBatch.SkippedCount);
    }

    [TestMethod]
    public async Task ImportBankStatementUseCase_SkipsRowsAlreadyPersistedFromPreviousImports()
    {
        const string sharedRow = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";
        const string newRow = "26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";

        var importBatchRepository = new FakeImportBatchRepository();
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var parser = new BankStatementParser();
        var transactionRepository = new FakeTransactionRepository();
        var useCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, duplicateCheck);

        var first = await useCase.ExecuteAsync("June2025-part1", sharedRow);
        var second = await useCase.ExecuteAsync("June2025-part2", string.Concat(sharedRow, "\n", newRow));

        Assert.AreEqual(1, first.ImportedCount);
        Assert.IsFalse(second.IsDuplicate);
        Assert.AreEqual(1, second.ImportedCount);
        Assert.AreEqual(1, second.SkippedDuplicateRowCount);
        Assert.AreEqual(1, second.Metrics.DuplicateExistingRowCount);
        Assert.IsTrue(second.Warnings.Any(static warning => warning.Code == BankStatementWarningCode.DuplicateRowInDatabase));
        Assert.AreEqual(2, transactionRepository.Items.Count);
    }

    [TestMethod]
    public async Task PreviewMatchedTransactionsUseCase_ReturnsOnlyRowsMatchedByRules()
    {
        const string content = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020\n26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";

        var categoryId = Guid.NewGuid();
        var categoryRepository = new FakeCategoryRepository();
        await categoryRepository.UpsertAsync(new Category
        {
            Id = categoryId,
            Name = "Services",
            ColorHex = "#64748B",
            IsSystem = true
        });

        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "OpenAI subscriptions",
                    Pattern = "OPENAI",
                    MatchType = RuleMatchType.Contains,
                    Priority = 100,
                    CategoryId = categoryId,
                    IsActive = true
                }
            ]
        };

        var useCase = new PreviewMatchedTransactionsUseCase(ruleRepository, categoryRepository, new BankStatementParser());
        var result = await useCase.ExecuteAsync(content);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(1, result.MatchedRows.Count);
        Assert.AreEqual(1, result.UnmatchedCount);
        Assert.AreEqual("Services", result.MatchedRows[0].CategoryName);
        Assert.AreEqual("OpenAI subscriptions", result.MatchedRows[0].RuleName);
    }

    [TestMethod]
    public async Task MergeMatchedTransactionsUseCase_PersistsOnlyMatchedRowsWithCategory()
    {
        const string content = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020\n26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";

        var categoryId = Guid.NewGuid();
        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "OpenAI subscriptions",
                    Pattern = "OPENAI",
                    MatchType = RuleMatchType.Contains,
                    Priority = 100,
                    CategoryId = categoryId,
                    IsActive = true
                }
            ]
        };

        var importBatchRepository = new FakeImportBatchRepository();
        var transactionRepository = new FakeTransactionRepository();
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var useCase = new MergeMatchedTransactionsUseCase(
            transactionRepository,
            importBatchRepository,
            ruleRepository,
            new BankStatementParser(),
            duplicateCheck);

        var result = await useCase.ExecuteAsync("June2025", content);

        Assert.IsFalse(result.IsDuplicate);
        Assert.AreEqual(1, result.MatchedCount);
        Assert.AreEqual(1, result.UnmatchedCount);
        Assert.AreEqual(1, result.Metrics.ImportedRowCount);
        Assert.AreEqual(1, transactionRepository.Items.Count);
        Assert.AreEqual(categoryId, transactionRepository.Items[0].CategoryId);
        Assert.AreEqual(1, importBatchRepository.StoredBatches.Count);
    }

    [TestMethod]
    public async Task GetLedgerEntriesUseCase_FiltersBySearchAndUncategorizedAndPaginates()
    {
        var groceriesCategoryId = Guid.NewGuid();
        var categoryRepository = new FakeCategoryRepository();
        await categoryRepository.UpsertAsync(new Category
        {
            Id = groceriesCategoryId,
            Name = "Groceries",
            ColorHex = "#22C55E",
            IsSystem = true
        });

        var transactionRepository = new FakeTransactionRepository
        {
            Items =
            [
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 30),
                    Description = "COMPRA TARJ. OPENAI",
                    NormalizedDescription = "COMPRA TARJ. OPENAI",
                    Amount = -20.72m,
                    CategoryId = null,
                    SourceAccount = "5402__7020"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 29),
                    Description = "SUPERMARKET MERCADONA",
                    NormalizedDescription = "SUPERMARKET MERCADONA",
                    Amount = -45.12m,
                    CategoryId = groceriesCategoryId,
                    SourceAccount = "5402__6021"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 28),
                    Description = "TRANSFERENCIA NOMINA",
                    NormalizedDescription = "TRANSFERENCIA NOMINA",
                    Amount = 2500m,
                    CategoryId = null,
                    SourceAccount = "ACTIVISION BLIZZ"
                }
            ]
        };

        var useCase = new GetLedgerEntriesUseCase(transactionRepository, categoryRepository);
        var query = new LedgerQuery(
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30),
            "OPENAI",
            OnlyUncategorized: true,
            Skip: 0,
            Take: 10);

        var filtered = await useCase.ExecuteAsync(query);
        Assert.AreEqual(1, filtered.TotalMatchedCount);
        Assert.AreEqual(1, filtered.Items.Count);
        StringAssert.Contains(filtered.Items[0].Description, "OPENAI");

        var paged = await useCase.ExecuteAsync(new LedgerQuery(
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30),
            SearchText: null,
            OnlyUncategorized: false,
            Skip: 1,
            Take: 1));

        Assert.AreEqual(3, paged.TotalMatchedCount);
        Assert.AreEqual(1, paged.Items.Count);
        Assert.IsTrue(paged.AvailableSourceAccounts.Count >= 2);
    }

    [TestMethod]
    public async Task GetLedgerEntriesUseCase_AppliesSourceCategoryAndAmountFilters()
    {
        var groceriesCategoryId = Guid.NewGuid();
        var salaryCategoryId = Guid.NewGuid();

        var categoryRepository = new FakeCategoryRepository();
        await categoryRepository.UpsertAsync(new Category
        {
            Id = groceriesCategoryId,
            Name = "Groceries",
            ColorHex = "#22C55E",
            IsSystem = true
        });
        await categoryRepository.UpsertAsync(new Category
        {
            Id = salaryCategoryId,
            Name = "Salary",
            ColorHex = "#2563EB",
            IsSystem = true
        });

        var transactionRepository = new FakeTransactionRepository
        {
            Items =
            [
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 30),
                    Description = "SUPERMARKET",
                    NormalizedDescription = "SUPERMARKET",
                    Amount = -20.72m,
                    CategoryId = groceriesCategoryId,
                    SourceAccount = "5402__7020"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 29),
                    Description = "SMALL GROCERY",
                    NormalizedDescription = "SMALL GROCERY",
                    Amount = -3.00m,
                    CategoryId = groceriesCategoryId,
                    SourceAccount = "5402__7020"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 28),
                    Description = "SALARY",
                    NormalizedDescription = "SALARY",
                    Amount = 2500m,
                    CategoryId = salaryCategoryId,
                    SourceAccount = "ACTIVISION BLIZZ"
                }
            ]
        };

        var useCase = new GetLedgerEntriesUseCase(transactionRepository, categoryRepository);
        var filtered = await useCase.ExecuteAsync(new LedgerQuery(
            new DateOnly(2025, 6, 1),
            new DateOnly(2025, 6, 30),
            SearchText: null,
            OnlyUncategorized: false,
            Skip: 0,
            Take: 50,
            SourceAccount: "5402__7020",
            CategoryId: groceriesCategoryId,
            MinAmount: -25m,
            MaxAmount: -5m));

        Assert.AreEqual(1, filtered.TotalMatchedCount);
        Assert.AreEqual(1, filtered.Items.Count);
        Assert.AreEqual("SUPERMARKET", filtered.Items[0].Description);
        CollectionAssert.Contains(filtered.AvailableSourceAccounts.ToList(), "5402__7020");
    }

    [TestMethod]
    public async Task BuildDashboardSnapshotUseCase_ComputesKpiTrendBreakdownAndUncategorized()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var twoMonthsAgoStart = currentMonthStart.AddMonths(-2);
        var groceriesId = Guid.NewGuid();
        var categoryRepository = new FakeCategoryRepository();
        await categoryRepository.UpsertAsync(new Category
        {
            Id = groceriesId,
            Name = "Groceries",
            ColorHex = "#22C55E",
            IsSystem = true
        });

        var transactionRepository = new FakeTransactionRepository
        {
            Items =
            [
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = currentMonthStart.AddDays(2),
                    Description = "Salary Current",
                    Amount = 3000m,
                    CategoryId = null,
                    NormalizedDescription = "SALARY CURRENT"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = currentMonthStart.AddDays(5),
                    Description = "Supermarket",
                    Amount = -120m,
                    CategoryId = groceriesId,
                    NormalizedDescription = "SUPERMARKET"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = previousMonthStart.AddDays(4),
                    Description = "Unknown charge",
                    Amount = -50m,
                    CategoryId = null,
                    NormalizedDescription = "UNKNOWN CHARGE"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = previousMonthStart.AddDays(8),
                    Description = "Streaming Subscription",
                    Amount = -25m,
                    CategoryId = null,
                    NormalizedDescription = "STREAMING SUBSCRIPTION"
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = twoMonthsAgoStart.AddDays(8),
                    Description = "Streaming Subscription",
                    Amount = -24m,
                    CategoryId = null,
                    NormalizedDescription = "STREAMING SUBSCRIPTION"
                }
            ]
        };

        var useCase = new BuildDashboardSnapshotUseCase(transactionRepository, categoryRepository);
        var snapshot = await useCase.ExecuteAsync(twoMonthsAgoStart, currentMonthStart.AddMonths(1), trendMonths: 3, uncategorizedLimit: 10);

        Assert.AreEqual(5, snapshot.Kpi.TotalTransactions);
        Assert.AreEqual(3000m, snapshot.Kpi.TotalIncome);
        Assert.AreEqual(219m, snapshot.Kpi.TotalExpenses);
        Assert.AreEqual(2781m, snapshot.Kpi.NetAmount);
        Assert.AreEqual(4, snapshot.Kpi.UncategorizedCount);
        Assert.AreEqual(99m, snapshot.Kpi.UncategorizedAmount);
        Assert.AreEqual(96m, snapshot.Kpi.SavingsRatePercent);

        Assert.AreEqual(3, snapshot.MonthlyTrend.Count);
        Assert.IsTrue(snapshot.MonthlyTrend.Any(point => point.Income > 0));
        Assert.IsTrue(snapshot.CategoryBreakdown.Any(item => item.CategoryName == "Groceries"));
        Assert.AreEqual(4, snapshot.UncategorizedQueue.Count);
        Assert.AreEqual(currentMonthStart, snapshot.CurrentVsPreviousMonth.Current.Month);
        Assert.AreEqual(3000m, snapshot.CurrentVsPreviousMonth.Current.Income);
        Assert.AreEqual(120m, snapshot.CurrentVsPreviousMonth.Current.Expenses);
        Assert.AreEqual(2880m, snapshot.CurrentVsPreviousMonth.Current.Net);
        Assert.AreEqual(previousMonthStart, snapshot.CurrentVsPreviousMonth.Previous.Month);
        Assert.AreEqual(75m, snapshot.CurrentVsPreviousMonth.Previous.Expenses);
        Assert.AreEqual(1, snapshot.Coverage.CategorizedCount);
        Assert.AreEqual(4, snapshot.Coverage.UncategorizedCount);
        Assert.AreEqual("Supermarket", snapshot.LargestExpenses[0].Description);
        Assert.AreEqual("Streaming Subscription", snapshot.RecurringExpenses[0].Description);
    }

    [TestMethod]
    public async Task Workflow_ImportThenAutoCategorizeThenManualOverride_IsDeterministic()
    {
        const string row = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";
        var groceriesCategory = Guid.NewGuid();
        var utilitiesCategory = Guid.NewGuid();

        var importBatchRepository = new FakeImportBatchRepository();
        var transactionRepository = new FakeTransactionRepository();
        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Exact OpenAI",
                    Pattern = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                    MatchType = RuleMatchType.Exact,
                    Priority = 100,
                    CategoryId = groceriesCategory
                }
            ]
        };
        var manualOverrideRepository = new FakeManualOverrideRepository();
        var categoryRepository = new FakeCategoryRepository();

        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var parser = new BankStatementParser();
        var importUseCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, parser, duplicateCheck);
        var applyRulesUseCase = new ApplyCategorizationRulesUseCase(transactionRepository, ruleRepository, manualOverrideRepository, categoryRepository);
        var manualRecategorizationUseCase = new ManualRecategorizationUseCase(transactionRepository, manualOverrideRepository);

        var importResult = await importUseCase.ExecuteAsync("June2025", row);
        Assert.AreEqual(1, importResult.ImportedCount);

        var applyResult = await applyRulesUseCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));
        Assert.AreEqual(1, applyResult.CategorizedCount);

        var transaction = transactionRepository.Items.Single();
        Assert.AreEqual(groceriesCategory, transaction.CategoryId);

        var manualResult = await manualRecategorizationUseCase.ExecuteAsync([transaction.Id], utilitiesCategory, "User correction");
        Assert.AreEqual(1, manualResult.ChangedCount);

        var applyResultAgain = await applyRulesUseCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));
        Assert.AreEqual(1, applyResultAgain.SkippedByManualOverrideCount);

        var finalTransaction = transactionRepository.Items.Single();
        Assert.AreEqual(utilitiesCategory, finalTransaction.CategoryId);
    }

    [TestMethod]
    public async Task ApplyCategorizationRulesUseCase_AppliesBestRuleByDeterministicPrecedence()
    {
        var groceries = Guid.NewGuid();
        var salaryCategory = Guid.NewGuid();

        var transactionRepository = new FakeTransactionRepository
        {
            Items =
            [
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 30),
                    Description = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                    Amount = -20.72m
                },
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 26),
                    Description = "TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID",
                    Amount = 2666.89m
                }
            ]
        };

        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Contains OpenAI",
                    Pattern = "OPENAI",
                    MatchType = RuleMatchType.Contains,
                    Priority = 100,
                    CategoryId = Guid.NewGuid()
                },
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Exact OpenAI",
                    Pattern = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                    MatchType = RuleMatchType.Exact,
                    Priority = 1,
                    CategoryId = groceries
                },
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Contains Salary",
                    Pattern = "NOMINA",
                    MatchType = RuleMatchType.Contains,
                    Priority = 50,
                    CategoryId = salaryCategory
                }
            ]
        };

        var manualOverrideRepository = new FakeManualOverrideRepository();
        var categoryRepository = new FakeCategoryRepository();
        var useCase = new ApplyCategorizationRulesUseCase(transactionRepository, ruleRepository, manualOverrideRepository, categoryRepository);

        var result = await useCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));

        Assert.AreEqual(2, result.ProcessedCount);
        Assert.AreEqual(2, result.CategorizedCount);
        Assert.AreEqual(0, result.SkippedByManualOverrideCount);

        var openAi = transactionRepository.Items.Single(item => item.Description.Contains("OPENAI", StringComparison.Ordinal));
        Assert.AreEqual(groceries, openAi.CategoryId);
        var salary = transactionRepository.Items.Single(item => item.Description.Contains("NOMINA", StringComparison.Ordinal));
        Assert.AreEqual(salaryCategory, salary.CategoryId);
    }

    [TestMethod]
    public async Task ApplyCategorizationRulesUseCase_SkipsTransactionsWithManualOverrides()
    {
        var categoryId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = new DateOnly(2025, 6, 20),
            Description = "IMPUESTOS Y TASAS RECIBO REFERENCIA CATASTRAL 7160028VH5776S0015SE- MADRID",
            Amount = -155.03m
        };

        var transactionRepository = new FakeTransactionRepository { Items = [transaction] };
        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Contains Impuestos",
                    Pattern = "IMPUESTOS",
                    MatchType = RuleMatchType.Contains,
                    Priority = 1,
                    CategoryId = categoryId
                }
            ]
        };

        var manualOverrideRepository = new FakeManualOverrideRepository
        {
            OverridesByTransactionId =
            {
                [transaction.Id] =
                [
                    new ManualOverride
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transaction.Id,
                        CategoryId = Guid.NewGuid(),
                        Reason = "User override"
                    }
                ]
            }
        };

        var categoryRepository = new FakeCategoryRepository();
        var useCase = new ApplyCategorizationRulesUseCase(transactionRepository, ruleRepository, manualOverrideRepository, categoryRepository);
        var result = await useCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));

        Assert.AreEqual(1, result.ProcessedCount);
        Assert.AreEqual(0, result.CategorizedCount);
        Assert.AreEqual(1, result.SkippedByManualOverrideCount);
        Assert.IsNull(transactionRepository.Items[0].CategoryId);
    }

    [TestMethod]
    public async Task ApplyCategorizationRulesUseCase_AutoAssignsIncomeCategory_ForPositiveTransactionsWithoutRuleMatch()
    {
        var incomeCategoryId = Guid.NewGuid();
        var transactionRepository = new FakeTransactionRepository
        {
            Items =
            [
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 7, 1),
                    Description = "TRANSFERENCIA RECIBIDA DE NOMINA",
                    Amount = 2500m
                }
            ]
        };

        var ruleRepository = new FakeRuleRepository { Rules = [] };
        var manualOverrideRepository = new FakeManualOverrideRepository();
        var categoryRepository = new FakeCategoryRepository();
        await categoryRepository.UpsertAsync(new Category
        {
            Id = incomeCategoryId,
            Name = "Income",
            ColorHex = "#22C55E",
            IsSystem = true
        });

        var useCase = new ApplyCategorizationRulesUseCase(transactionRepository, ruleRepository, manualOverrideRepository, categoryRepository);
        var result = await useCase.ExecuteAsync(new DateOnly(2025, 7, 1), new DateOnly(2025, 7, 31));

        Assert.AreEqual(1, result.ProcessedCount);
        Assert.AreEqual(1, result.CategorizedCount);
        Assert.AreEqual(0, result.NoMatchCount);
        Assert.AreEqual(incomeCategoryId, transactionRepository.Items[0].CategoryId);
    }

    [TestMethod]
    public async Task CheckImportDuplicateUseCase_NormalizesLineEndingsBeforeHashing()
    {
        const string unixContent = "30/06/2025|COMPRA TARJ. OPENAI|30/06/2025|-20.72|22482.40||5402__7020\n26/06/2025|TRANSFERENCIA NOMINA|26/06/2025|2666.89|23095.08||";

        var importBatchRepository = new FakeImportBatchRepository();
        var useCase = new CheckImportDuplicateUseCase(importBatchRepository);

        var first = await useCase.ExecuteAsync(" June2025 ", unixContent);
        await importBatchRepository.UpsertAsync(new ImportBatch
        {
            Id = Guid.NewGuid(),
            SourceName = first.SourceName,
            FileHash = first.FileHash,
            ImportedAtUtc = DateTimeOffset.UtcNow,
            ImportedCount = 2,
            SkippedCount = 0
        });

        var second = await useCase.ExecuteAsync("June2025", unixContent.Replace("\n", "\r\n", StringComparison.Ordinal));

        Assert.AreEqual("June2025", first.SourceName);
        Assert.AreEqual(first.FileHash, second.FileHash);
        Assert.IsTrue(second.IsDuplicate);
        Assert.IsTrue(second.ExistingBatchId.HasValue);
    }

    [TestMethod]
    public async Task ImportBankStatementUseCase_TracksMixedDuplicateAndExistingRowsInOneImport()
    {
        const string sharedRow = "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020";
        const string newExpenseRow = "11/07/2025|PAGO BIZUM AMIGO CENA|11/07/2025|-18.50|21035.10||";
        const string newIncomeRow = "26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||";

        var importBatchRepository = new FakeImportBatchRepository();
        var transactionRepository = new FakeTransactionRepository
        {
            Items = [CreatePersistedTransactionFromRow(sharedRow)]
        };
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var useCase = new ImportBankStatementUseCase(transactionRepository, importBatchRepository, new BankStatementParser(), duplicateCheck);

        var content = string.Concat(sharedRow, "\n", newExpenseRow, "\n", newExpenseRow, "\n", newIncomeRow);
        var result = await useCase.ExecuteAsync("July2025", content);

        Assert.IsFalse(result.IsDuplicate);
        Assert.AreEqual(2, result.ImportedCount);
        Assert.AreEqual(2, result.SkippedDuplicateRowCount);
        Assert.AreEqual(4, result.Metrics.ParsedRowCount);
        Assert.AreEqual(1, result.Metrics.DuplicateRowCount);
        Assert.AreEqual(1, result.Metrics.DuplicateExistingRowCount);
        Assert.AreEqual(2, result.Metrics.ImportedRowCount);
        Assert.AreEqual(2, result.Warnings.Count);
        Assert.IsTrue(result.Warnings.Any(static warning => warning.Code == BankStatementWarningCode.DuplicateRowInFile));
        Assert.IsTrue(result.Warnings.Any(static warning => warning.Code == BankStatementWarningCode.DuplicateRowInDatabase));
        Assert.AreEqual(3, transactionRepository.Items.Count);

        var batch = importBatchRepository.StoredBatches.Single();
        Assert.AreEqual(2, batch.ImportedCount);
        Assert.AreEqual(2, batch.SkippedCount);
    }

    [TestMethod]
    public async Task MergeMatchedTransactionsUseCase_WhenNoRulesMatch_PersistsNothingAndTracksUnmatchedRows()
    {
        const string content = "30/06/2025|COMPRA TARJ. OPENAI|30/06/2025|-20.72|22482.40||5402__7020\n26/06/2025|TRANSFERENCIA NOMINA|26/06/2025|2666.89|23095.08||";

        var importBatchRepository = new FakeImportBatchRepository();
        var transactionRepository = new FakeTransactionRepository();
        var ruleRepository = new FakeRuleRepository { Rules = [] };
        var duplicateCheck = new CheckImportDuplicateUseCase(importBatchRepository);
        var useCase = new MergeMatchedTransactionsUseCase(
            transactionRepository,
            importBatchRepository,
            ruleRepository,
            new BankStatementParser(),
            duplicateCheck);

        var result = await useCase.ExecuteAsync("NoMatches", content);

        Assert.IsFalse(result.IsDuplicate);
        Assert.AreEqual(0, result.MatchedCount);
        Assert.AreEqual(2, result.UnmatchedCount);
        Assert.AreEqual(0, result.Metrics.ImportedRowCount);
        Assert.AreEqual(0, result.SkippedDuplicateRowCount);
        Assert.AreEqual(0, transactionRepository.Items.Count);

        var batch = importBatchRepository.StoredBatches.Single();
        Assert.AreEqual(0, batch.ImportedCount);
        Assert.AreEqual(2, batch.SkippedCount);
    }

    [TestMethod]
    public async Task ApplyCategorizationRulesUseCase_ReapplyIsIdempotentAfterFirstSuccessfulPass()
    {
        var categoryId = Guid.NewGuid();
        var transactionRepository = new FakeTransactionRepository
        {
            Items =
            [
                new Transaction
                {
                    Id = Guid.NewGuid(),
                    BookingDate = new DateOnly(2025, 6, 30),
                    Description = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                    NormalizedDescription = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                    Amount = -20.72m
                }
            ]
        };

        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Exact OpenAI",
                    Pattern = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                    MatchType = RuleMatchType.Exact,
                    Priority = 100,
                    CategoryId = categoryId,
                    IsActive = true
                }
            ]
        };

        var useCase = new ApplyCategorizationRulesUseCase(
            transactionRepository,
            ruleRepository,
            new FakeManualOverrideRepository(),
            new FakeCategoryRepository());

        var first = await useCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));
        var second = await useCase.ExecuteAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));

        Assert.AreEqual(1, first.CategorizedCount);
        Assert.AreEqual(0, second.CategorizedCount);
        Assert.AreEqual(0, second.SkippedByManualOverrideCount);
        Assert.AreEqual(0, second.NoMatchCount);
        Assert.AreEqual(categoryId, transactionRepository.Items[0].CategoryId);
    }

    [TestMethod]
    public async Task DeleteAllRulesUseCase_RemovesAllActiveRules()
    {
        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Rule A",
                    Pattern = "OPENAI",
                    MatchType = RuleMatchType.Contains,
                    Priority = 10,
                    CategoryId = Guid.NewGuid(),
                    IsActive = true
                },
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Rule B",
                    Pattern = "UBER",
                    MatchType = RuleMatchType.Contains,
                    Priority = 5,
                    CategoryId = Guid.NewGuid(),
                    IsActive = true
                }
            ]
        };

        var useCase = new DeleteAllRulesUseCase(ruleRepository);
        var result = await useCase.ExecuteAsync();

        Assert.AreEqual(2, result.DeletedCount);
        Assert.AreEqual(0, ruleRepository.Rules.Count);
    }

    [TestMethod]
    public async Task PreviewPotentialRulesUseCase_ReturnsRowsWhenNoRules()
    {
        const string content = "30/06/2025|COMPRA TARJ. OPENAI|30/06/2025|-20.72|22482.40||5402__7020";

        var ruleRepository = new FakeRuleRepository { Rules = [] };
        var parser = new BankStatementParser();
        var useCase = new PreviewPotentialRulesUseCase(ruleRepository, parser);

        var result = await useCase.ExecuteAsync(content);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(1, result.PotentialRows.Count);
        Assert.AreEqual("COMPRA TARJ. OPENAI", result.PotentialRows[0].Description);
    }

    [TestMethod]
    public async Task PreviewPotentialRulesUseCase_ExcludesRowsMatchedByRules()
    {
        const string content = "30/06/2025|COMPRA TARJ. OPENAI|30/06/2025|-20.72|22482.40||5402__7020";

        var ruleRepository = new FakeRuleRepository
        {
            Rules =
            [
                new CategorizationRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Contains OpenAI",
                    Pattern = "OPENAI",
                    MatchType = RuleMatchType.Contains,
                    Priority = 10,
                    CategoryId = Guid.NewGuid(),
                    IsActive = true
                }
            ]
        };
        var parser = new BankStatementParser();
        var useCase = new PreviewPotentialRulesUseCase(ruleRepository, parser);

        var result = await useCase.ExecuteAsync(content);

        Assert.AreEqual(0, result.Errors.Count);
        Assert.AreEqual(0, result.PotentialRows.Count);
    }

    [TestMethod]
    public async Task ManualRecategorizationUseCase_UpdatesCategoryAndAddsOverrideHistory()
    {
        var tx1 = new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = new DateOnly(2025, 6, 30),
            Description = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
            Amount = -20.72m,
            CategoryId = null
        };
        var tx2 = new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = new DateOnly(2025, 6, 26),
            Description = "TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID",
            Amount = 2666.89m,
            CategoryId = null
        };

        var categoryId = Guid.NewGuid();
        var transactionRepository = new FakeTransactionRepository { Items = [tx1, tx2] };
        var overrideRepository = new FakeManualOverrideRepository();
        var useCase = new ManualRecategorizationUseCase(transactionRepository, overrideRepository);

        var result = await useCase.ExecuteAsync([tx1.Id, tx2.Id], categoryId, "Corrected by user");

        Assert.AreEqual(2, result.RequestedCount);
        Assert.AreEqual(2, result.ChangedCount);
        Assert.AreEqual(0, result.AlreadySameCategoryCount);
        Assert.AreEqual(0, result.NotFoundCount);

        Assert.IsTrue(transactionRepository.Items.All(item => item.CategoryId == categoryId));
        Assert.AreEqual(1, overrideRepository.OverridesByTransactionId[tx1.Id].Count);
        Assert.AreEqual(1, overrideRepository.OverridesByTransactionId[tx2.Id].Count);
        Assert.AreEqual("Corrected by user", overrideRepository.OverridesByTransactionId[tx1.Id][0].Reason);
    }

    [TestMethod]
    public async Task ManualRecategorizationUseCase_TracksNotFoundAndAlreadyAssigned()
    {
        var categoryId = Guid.NewGuid();
        var existing = new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = new DateOnly(2025, 6, 20),
            Description = "IMPUESTOS Y TASAS",
            Amount = -155.03m,
            CategoryId = categoryId
        };

        var missingId = Guid.NewGuid();
        var transactionRepository = new FakeTransactionRepository { Items = [existing] };
        var overrideRepository = new FakeManualOverrideRepository();
        var useCase = new ManualRecategorizationUseCase(transactionRepository, overrideRepository);

        var result = await useCase.ExecuteAsync([existing.Id, missingId], categoryId, "No-op");

        Assert.AreEqual(2, result.RequestedCount);
        Assert.AreEqual(0, result.ChangedCount);
        Assert.AreEqual(1, result.AlreadySameCategoryCount);
        Assert.AreEqual(1, result.NotFoundCount);
        Assert.IsFalse(overrideRepository.OverridesByTransactionId.ContainsKey(existing.Id));
    }

    private static Transaction CreatePersistedTransactionFromRow(string row)
    {
        var parser = new BankStatementParser();
        var parse = parser.Parse(row);
        Assert.AreEqual(1, parse.Rows.Count);

        var parsedRow = parse.Rows[0];
        var now = DateTimeOffset.UtcNow;

        return new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = parsedRow.BookingDate,
            ValueDate = parsedRow.ValueDate,
            Description = parsedRow.Description,
            NormalizedDescription = DescriptionNormalizer.Normalize(parsedRow.Description),
            TransactionFingerprint = TransactionFingerprintBuilder.Build(
                parsedRow.BookingDate,
                parsedRow.ValueDate,
                parsedRow.Description,
                parsedRow.Amount,
                parsedRow.RunningBalance,
                parsedRow.ExternalReference,
                parsedRow.SourceAccount),
            Amount = parsedRow.Amount,
            Currency = "EUR",
            SourceAccount = parsedRow.SourceAccount,
            ExternalReference = parsedRow.ExternalReference,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }

    private sealed class FakeMigrationRunner : IMigrationRunner
    {
        public int CallCount { get; private set; }

        public Task EnsureCreatedAndMigratedAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        public List<Category> Items { get; } = [];

        public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Category>>(Items.ToList());

        public Task UpsertAsync(Category category, CancellationToken cancellationToken = default)
        {
            var existing = Items.FindIndex(item => item.Id == category.Id);
            if (existing >= 0)
            {
                Items[existing] = category;
            }
            else
            {
                Items.Add(category);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(item => item.Id == categoryId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeImportBatchRepository : IImportBatchRepository
    {
        private readonly Dictionary<string, ImportBatch> _items = new(StringComparer.Ordinal);

        public IReadOnlyCollection<ImportBatch> StoredBatches => _items.Values;

        public Task<ImportBatch?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(fileHash, out var batch);
            return Task.FromResult(batch);
        }

        public Task UpsertAsync(ImportBatch batch, CancellationToken cancellationToken = default)
        {
            _items[batch.FileHash] = batch;
            return Task.CompletedTask;
        }

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            var deleted = _items.Count;
            _items.Clear();
            return Task.FromResult(deleted);
        }
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Items { get; set; } = [];

        public Task UpsertManyAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            Items.AddRange(transactions);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateOnly fromInclusive, DateOnly toInclusive, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Transaction>>(Items.ToList());

        public Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(item => item.Id == transactionId));

        public Task SetCategoryAsync(Guid transactionId, Guid? categoryId, CancellationToken cancellationToken = default)
        {
            var index = Items.FindIndex(item => item.Id == transactionId);
            if (index >= 0)
            {
                Items[index] = Items[index] with { CategoryId = categoryId };
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<string>> GetExistingFingerprintsAsync(IReadOnlyCollection<string> fingerprints, CancellationToken cancellationToken = default)
        {
            var existing = Items
                .Select(item => item.TransactionFingerprint)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Intersect(fingerprints, StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            return Task.FromResult<IReadOnlySet<string>>(existing);
        }

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            var deleted = Items.Count;
            Items.Clear();
            return Task.FromResult(deleted);
        }
    }

    private sealed class FakeRuleRepository : IRuleRepository
    {
        public List<CategorizationRule> Rules { get; set; } = [];

        public Task<IReadOnlyList<CategorizationRule>> GetActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CategorizationRule>>(Rules.Where(static rule => rule.IsActive).ToList());

        public Task UpsertAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
        {
            var index = Rules.FindIndex(item => item.Id == rule.Id);
            if (index >= 0)
            {
                Rules[index] = rule;
            }
            else
            {
                Rules.Add(rule);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
        {
            Rules.RemoveAll(item => item.Id == ruleId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeManualOverrideRepository : IManualOverrideRepository
    {
        public Dictionary<Guid, List<ManualOverride>> OverridesByTransactionId { get; } = [];

        public Task AddAsync(ManualOverride manualOverride, CancellationToken cancellationToken = default)
        {
            if (!OverridesByTransactionId.TryGetValue(manualOverride.TransactionId, out var list))
            {
                list = [];
                OverridesByTransactionId[manualOverride.TransactionId] = list;
            }

            list.Add(manualOverride);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ManualOverride>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        {
            OverridesByTransactionId.TryGetValue(transactionId, out var list);
            return Task.FromResult<IReadOnlyList<ManualOverride>>(list?.ToList() ?? []);
        }

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            var deleted = OverridesByTransactionId.Values.Sum(static items => items.Count);
            OverridesByTransactionId.Clear();
            return Task.FromResult(deleted);
        }
    }
}

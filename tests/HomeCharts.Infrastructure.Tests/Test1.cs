using HomeCharts.Domain.Model;
using HomeCharts.Domain.Categorization;
using HomeCharts.Infrastructure.Persistence.Sqlite;
using System.Globalization;

namespace HomeCharts.Infrastructure.Tests;

[TestClass]
public sealed class InfrastructurePersistenceTests
{
    [TestMethod]
    public async Task MigrationRunner_CreatesSchemaAndVersionEntry()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);

            await migrationRunner.EnsureCreatedAndMigratedAsync();

            await using var connection = await factory.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM __schema_migrations;";
            var result = await command.ExecuteScalarAsync();
            var count = Convert.ToInt32(result);

            Assert.IsTrue(count >= 1);
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    [TestMethod]
    public async Task CategoryRepository_UpsertAndRead_RoundTrips()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var repository = new SqliteCategoryRepository(factory);
            var category = new Category
            {
                Name = "Transport",
                ColorHex = "#0EA5E9",
                IsSystem = false
            };

            await repository.UpsertAsync(category);
            var all = await repository.GetAllAsync();

            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("Transport", all[0].Name);
            Assert.AreEqual("#0EA5E9", all[0].ColorHex);
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    [TestMethod]
    public async Task TransactionRepository_PersistsCopiedBankRecipeRows()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var repository = new SqliteTransactionRepository(factory);
            var copiedRows = new[]
            {
                "30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020",
                "26/06/2025|TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID|26/06/2025|2666.89|23095.08||"
            };

            var transactions = copiedRows.Select(ToTransaction).ToArray();
            await repository.UpsertManyAsync(transactions);

            var all = await repository.GetByDateRangeAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));

            Assert.AreEqual(2, all.Count);

            var openAi = all.Single(item => item.Description.Contains("OPENAI", StringComparison.Ordinal));
            Assert.AreEqual(-20.72m, openAi.Amount);
            Assert.AreEqual("5402__7020", openAi.SourceAccount);
            Assert.IsNull(openAi.ExternalReference);
            Assert.AreEqual(
                "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                openAi.NormalizedDescription);

            var salary = all.Single(item => item.Description.Contains("NOMINA MAY 2025", StringComparison.Ordinal));
            Assert.AreEqual(2666.89m, salary.Amount);
            Assert.IsNull(salary.SourceAccount);
            Assert.IsNull(salary.ExternalReference);
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    [TestMethod]
    public async Task TransactionRepository_AllowsEmptyOptionalBankFields()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var repository = new SqliteTransactionRepository(factory);
            var row = "20/06/2025|IMPUESTOS Y TASAS RECIBO REFERENCIA CATASTRAL 7160028VH5776S0015SE- MADRID|20/06/2025|-155.03|22862.37||";
            var transaction = ToTransaction(row);

            await repository.UpsertManyAsync([transaction]);
            var saved = await repository.GetByIdAsync(transaction.Id);

            Assert.IsNotNull(saved);
            Assert.AreEqual(-155.03m, saved.Amount);
            Assert.IsNull(saved.SourceAccount);
            Assert.IsNull(saved.ExternalReference);
            Assert.AreEqual(new DateOnly(2025, 6, 20), saved.BookingDate);
            Assert.AreEqual(new DateOnly(2025, 6, 20), saved.ValueDate);
            Assert.AreEqual(
                "IMPUESTOS Y TASAS RECIBO REFERENCIA CATASTRAL 7160028VH5776S0015SE- MADRID",
                saved.NormalizedDescription);
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    [TestMethod]
    public async Task ManualOverrideAndTransactionCategory_PersistTogether()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var categoryRepository = new SqliteCategoryRepository(factory);
            var transactionRepository = new SqliteTransactionRepository(factory);
            var overrideRepository = new SqliteManualOverrideRepository(factory);

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Utilities",
                ColorHex = "#8B5CF6",
                IsSystem = false,
                CreatedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            await categoryRepository.UpsertAsync(category);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                BookingDate = new DateOnly(2025, 6, 30),
                ValueDate = new DateOnly(2025, 6, 30),
                Description = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                Amount = -20.72m,
                Currency = "EUR",
                CreatedUtc = DateTimeOffset.UtcNow,
                UpdatedUtc = DateTimeOffset.UtcNow
            };

            await transactionRepository.UpsertManyAsync([transaction]);
            await transactionRepository.SetCategoryAsync(transaction.Id, category.Id);

            await overrideRepository.AddAsync(new ManualOverride
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                CategoryId = category.Id,
                Reason = "Manual category correction",
                CreatedUtc = DateTimeOffset.UtcNow
            });

            var savedTransaction = await transactionRepository.GetByIdAsync(transaction.Id);
            var overrides = await overrideRepository.GetByTransactionIdAsync(transaction.Id);

            Assert.IsNotNull(savedTransaction);
            Assert.AreEqual(category.Id, savedTransaction.CategoryId);
            Assert.AreEqual(1, overrides.Count);
            Assert.AreEqual(category.Id, overrides[0].CategoryId);
            Assert.AreEqual("Manual category correction", overrides[0].Reason);
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    [TestMethod]
    public async Task TransactionRepository_FindsExistingFingerprints()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var repository = new SqliteTransactionRepository(factory);
            var transaction = ToTransaction("30/06/2025|COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO|30/06/2025|-20.72|22482.40||5402__7020");
            await repository.UpsertManyAsync([transaction]);

            var existing = await repository.GetExistingFingerprintsAsync([transaction.TransactionFingerprint, "missing"]);

            Assert.AreEqual(1, existing.Count);
            Assert.IsTrue(existing.Contains(transaction.TransactionFingerprint));
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    private static Transaction ToTransaction(string row)
    {
        var columns = row.Split('|');
        if (columns.Length != 7)
        {
            throw new InvalidOperationException("Expected 7 pipe-delimited columns.");
        }

        var bookingDate = DateOnly.ParseExact(columns[0], "dd/MM/yyyy", CultureInfo.InvariantCulture);
        var description = columns[1].Trim();
        var valueDate = DateOnly.ParseExact(columns[2], "dd/MM/yyyy", CultureInfo.InvariantCulture);
        var amount = decimal.Parse(columns[3], CultureInfo.InvariantCulture);
        var externalReference = string.IsNullOrWhiteSpace(columns[5]) ? null : columns[5].Trim();
        var sourceAccount = string.IsNullOrWhiteSpace(columns[6]) ? null : columns[6].Trim();
        var now = DateTimeOffset.UtcNow;

        return new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = bookingDate,
            ValueDate = valueDate,
            Description = description,
            NormalizedDescription = DescriptionNormalizer.Normalize(description),
            TransactionFingerprint = TransactionFingerprintBuilder.Build(
                bookingDate,
                valueDate,
                description,
                amount,
                0m,
                externalReference,
                sourceAccount),
            Amount = amount,
            Currency = "EUR",
            ExternalReference = externalReference,
            SourceAccount = sourceAccount,
            CreatedUtc = now,
            UpdatedUtc = now
        };
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

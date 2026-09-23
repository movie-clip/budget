using HomeCharts.Domain.Model;
using HomeCharts.Infrastructure.Persistence.Sqlite;

namespace HomeCharts.Infrastructure.Tests;

[TestClass]
public sealed class SqliteTransactionRepositoryTests
{
    [TestMethod]
    public async Task GetByDateRangeAsync_RowMarkedDeletedInRange_IsExcludedWhileSiblingRowIsReturned()
    {
        var dbPath = CreateTempDatabasePath();

        try
        {
            var factory = new SqliteConnectionFactory(new SqliteOptions { DatabasePath = dbPath });
            var migrationRunner = new SqliteMigrationRunner(factory);
            await migrationRunner.EnsureCreatedAndMigratedAsync();

            var transactionRepository = new SqliteTransactionRepository(factory);

            var deletedTransaction = NewTransaction(new DateOnly(2025, 6, 15), -20m, isDeleted: true);
            var activeTransaction = NewTransaction(new DateOnly(2025, 6, 16), -30m, isDeleted: false);

            await transactionRepository.UpsertManyAsync([deletedTransaction, activeTransaction]);

            var result = await transactionRepository.GetByDateRangeAsync(new DateOnly(2025, 6, 1), new DateOnly(2025, 6, 30));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(activeTransaction.Id, result.Single().Id);
            Assert.IsFalse(result.Any(transaction => transaction.Id == deletedTransaction.Id));
        }
        finally
        {
            CleanupDatabaseFiles(dbPath);
        }
    }

    private static Transaction NewTransaction(DateOnly bookingDate, decimal amount, bool isDeleted)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = bookingDate,
            Description = "PAGO BIZUM AMIGO CENA",
            NormalizedDescription = "PAGO BIZUM AMIGO CENA",
            Amount = amount,
            TransactionFingerprint = Guid.NewGuid().ToString("N"),
            IsDeleted = isDeleted
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

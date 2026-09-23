using HomeCharts.Application.UseCases;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.Tests;

[TestClass]
public sealed class BuildDashboardSnapshotUseCaseTrendAnchorTests
{
    [TestMethod]
    public async Task ExecuteAsync_OnlyHistoricalTransactions_AnchorsTrendWindowAtMaxBookingDate()
    {
        var transactionRepository = new InMemoryTransactionRepository();
        var categoryRepository = new InMemoryCategoryRepository();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var latestBookingDate = today.AddMonths(-2);

        await transactionRepository.UpsertManyAsync([
            NewTransaction(latestBookingDate.AddMonths(-3), -40m),
            NewTransaction(latestBookingDate, -60m)
        ]);

        var useCase = new BuildDashboardSnapshotUseCase(transactionRepository, categoryRepository);

        var snapshot = await useCase.ExecuteAsync(
            today.AddYears(-5),
            today.AddDays(1),
            trendMonths: 12);

        var expectedAnchorMonth = new DateOnly(latestBookingDate.Year, latestBookingDate.Month, 1);
        Assert.AreEqual(expectedAnchorMonth, snapshot.MonthlyTrend.Last().Month);
    }

    [TestMethod]
    public async Task ExecuteAsync_TransactionBookedInTheFuture_CapsTrendWindowAnchorAtToday()
    {
        var transactionRepository = new InMemoryTransactionRepository();
        var categoryRepository = new InMemoryCategoryRepository();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var futureBookingDate = today.AddMonths(3);

        await transactionRepository.UpsertManyAsync([
            NewTransaction(today.AddMonths(-1), -25m),
            NewTransaction(futureBookingDate, -999m)
        ]);

        var useCase = new BuildDashboardSnapshotUseCase(transactionRepository, categoryRepository);

        var snapshot = await useCase.ExecuteAsync(
            today.AddYears(-5),
            today.AddYears(1),
            trendMonths: 12);

        var expectedAnchorMonth = new DateOnly(today.Year, today.Month, 1);
        Assert.AreEqual(expectedAnchorMonth, snapshot.MonthlyTrend.Last().Month);
        Assert.IsTrue(snapshot.MonthlyTrend.All(point => point.Month <= expectedAnchorMonth),
            "no trend point should extend past the capped (today) anchor month");
        Assert.IsFalse(snapshot.MonthlyTrend.Any(point => point.Expenses == 999m),
            "the future-booked transaction's amount must not appear in the emitted trend window");
    }

    private static Transaction NewTransaction(DateOnly bookingDate, decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = bookingDate,
            Description = "Test transaction",
            NormalizedDescription = "TEST TRANSACTION",
            Amount = amount,
            TransactionFingerprint = Guid.NewGuid().ToString("N")
        };
    }
}

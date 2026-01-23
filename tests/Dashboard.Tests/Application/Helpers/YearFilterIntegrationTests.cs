using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;

namespace Dashboard.Tests.Application.Helpers;

public class YearFilterIntegrationTests
{
    [Fact]
    public void YearFilter_FiltersTransactionsToSpecificYear()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction { Ticker = "AAPL", Date = new DateOnly(2022, 6, 15), Amount = 10, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "MSFT", Date = new DateOnly(2023, 3, 20), Amount = 5, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "GOOG", Date = new DateOnly(2023, 8, 10), Amount = 3, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "TSLA", Date = new DateOnly(2024, 1, 5), Amount = 2, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "AMZN", Date = new DateOnly(2024, 12, 31), Amount = 1, PurchasePrice = 100, TransactionCosts = 0 },
        };

        // Act - Filter to year 2023
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(2023);
        var filtered2023 = FilterHelper.FilterTransactions(transactions, null, startDate, endDate);

        // Assert
        Assert.Equal(2, filtered2023.Count);
        Assert.All(filtered2023, t => Assert.Equal(2023, t.Date.Year));
        Assert.Contains(filtered2023, t => t.Ticker == "MSFT");
        Assert.Contains(filtered2023, t => t.Ticker == "GOOG");
    }

    [Fact]
    public void YearFilter_WorksWithTickerFilter()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction { Ticker = "AAPL", Date = new DateOnly(2023, 6, 15), Amount = 10, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "AAPL", Date = new DateOnly(2023, 12, 20), Amount = 5, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "MSFT", Date = new DateOnly(2023, 8, 10), Amount = 3, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "AAPL", Date = new DateOnly(2024, 1, 5), Amount = 2, PurchasePrice = 100, TransactionCosts = 0 },
        };

        // Act - Filter to year 2023 and ticker AAPL
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(2023);
        var filtered = FilterHelper.FilterTransactions(transactions, "AAPL", startDate, endDate);

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, t => Assert.Equal("AAPL", t.Ticker));
        Assert.All(filtered, t => Assert.Equal(2023, t.Date.Year));
    }

    [Fact]
    public void YearFilter_FiltersLineChartDataPoints()
    {
        // Arrange
        var dataPoints = new List<DataPointDto>
        {
            new DataPointDto { Label = "2022-12-31", Value = 100 },
            new DataPointDto { Label = "2023-01-01", Value = 110 },
            new DataPointDto { Label = "2023-06-15", Value = 150 },
            new DataPointDto { Label = "2023-12-31", Value = 200 },
            new DataPointDto { Label = "2024-01-01", Value = 210 },
        };

        // Act - Filter to year 2023
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(2023);
        var filtered = FilterHelper.FilterLineChartDataPoints(dataPoints, startDate, endDate);

        // Assert
        Assert.Equal(3, filtered.Count);
        Assert.Contains(filtered, dp => dp.Label == "2023-01-01");
        Assert.Contains(filtered, dp => dp.Label == "2023-06-15");
        Assert.Contains(filtered, dp => dp.Label == "2023-12-31");
        Assert.DoesNotContain(filtered, dp => dp.Label == "2022-12-31");
        Assert.DoesNotContain(filtered, dp => dp.Label == "2024-01-01");
    }

    [Fact]
    public void YearFilter_HandlesEdgeCasesCorrectly()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction { Ticker = "AAPL", Date = new DateOnly(2023, 1, 1), Amount = 1, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "MSFT", Date = new DateOnly(2023, 12, 31), Amount = 1, PurchasePrice = 100, TransactionCosts = 0 },
        };

        // Act - Filter to year 2023
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(2023);
        var filtered = FilterHelper.FilterTransactions(transactions, null, startDate, endDate);

        // Assert - Both transactions should be included (boundary inclusive)
        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void YearFilter_ReturnsEmptyForYearWithNoTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction { Ticker = "AAPL", Date = new DateOnly(2023, 6, 15), Amount = 10, PurchasePrice = 100, TransactionCosts = 0 },
            new Transaction { Ticker = "MSFT", Date = new DateOnly(2024, 3, 20), Amount = 5, PurchasePrice = 100, TransactionCosts = 0 },
        };

        // Act - Filter to year 2022 (no transactions)
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(2022);
        var filtered = FilterHelper.FilterTransactions(transactions, null, startDate, endDate);

        // Assert
        Assert.Empty(filtered);
    }
}

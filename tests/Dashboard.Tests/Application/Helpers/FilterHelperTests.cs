using System.Globalization;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;

namespace Dashboard.Tests.Application.Helpers;

public class FilterHelperTests
{
    // -------- FilterTransactions --------
    [Fact]
    public void FilterTransactions_ReturnsAll_WhenNoFilters()
    {
        // Arrange
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = "MSFT", Date = new DateOnly(2025, 2, 1) },
            new TransactionDto { Ticker = "", Date = new DateOnly(2025, 3, 1) },
        };

        // Act
        var result = FilterHelper.FilterTransactions(tx, tickers: null);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Same(tx[0], result[0]);
        Assert.Same(tx[1], result[1]);
        Assert.Same(tx[2], result[2]);
    }

    [Fact]
    public void FilterTransactions_IgnoresFiltering_WhenTickersStringIsLiteralNull()
    {
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = "MSFT", Date = new DateOnly(2025, 2, 1) },
        };

        var result = FilterHelper.FilterTransactions(tx, tickers: "null");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterTransactions_FiltersByTickers_CaseInsensitive_CommaSeparated()
    {
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = "MSFT", Date = new DateOnly(2025, 1, 2) },
            new TransactionDto { Ticker = "GOOG", Date = new DateOnly(2025, 1, 3) },
        };

        var result = FilterHelper.FilterTransactions(tx, tickers: "msft, AaPl");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Ticker == "AAPL");
        Assert.Contains(result, t => t.Ticker == "MSFT");
        Assert.DoesNotContain(result, t => t.Ticker == "GOOG");
    }

    [Fact]
    public void FilterTransactions_ExcludesNullOrWhitespaceTickers_WhenFilteringByTickers()
    {
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = " ", Date = new DateOnly(2025, 1, 2) },
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 3) },
        };

        var result = FilterHelper.FilterTransactions(tx, tickers: "AAPL");

        Assert.Single(result);
        Assert.Equal("AAPL", result[0].Ticker);
    }

    [Fact]
    public void FilterTransactions_FiltersByStartAndEndDate_Inclusive()
    {
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = "MSFT", Date = new DateOnly(2025, 1, 15) },
            new TransactionDto { Ticker = "GOOG", Date = new DateOnly(2025, 2, 1) },
        };

        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2025, 1, 31);

        var result = FilterHelper.FilterTransactions(tx, tickers: null, startDate: start, endDate: end);

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.True(t.Date >= start && t.Date <= end));
    }

    [Fact]
    public void FilterTransactions_FiltersOnlyStart_WhenOnlyStartProvided()
    {
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = "MSFT", Date = new DateOnly(2025, 2, 1) },
        };

        var result = FilterHelper.FilterTransactions(tx, tickers: null, startDate: new DateOnly(2025, 2, 1));

        Assert.Single(result);
        Assert.Equal("MSFT", result[0].Ticker);
    }

    [Fact]
    public void FilterTransactions_FiltersOnlyEnd_WhenOnlyEndProvided()
    {
        var tx = new List<TransactionDto>
        {
            new TransactionDto { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1) },
            new TransactionDto { Ticker = "MSFT", Date = new DateOnly(2025, 2, 1) },
        };

        var result = FilterHelper.FilterTransactions(tx, tickers: null, endDate: new DateOnly(2025, 1, 31));

        Assert.Single(result);
        Assert.Equal("AAPL", result[0].Ticker);
    }

    // -------- GetMinMaxDatesFromTimeRange --------
    [Theory]
    [InlineData("1W")]
    [InlineData("1w")] // case-insensitive
    public void GetMinMaxDatesFromTimeRange_1W(string input)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (start, end) = FilterHelper.GetMinMaxDatesFromTimeRange(input);
        Assert.Equal(today.AddDays(-7), start);
        Assert.Equal(today, end);
    }

    [Fact]
    public void GetMinMaxDatesFromTimeRange_1M()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (start, end) = FilterHelper.GetMinMaxDatesFromTimeRange("1M");
        Assert.Equal(today.AddMonths(-1), start);
        Assert.Equal(today, end);
    }

    [Fact]
    public void GetMinMaxDatesFromTimeRange_3M()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (start, end) = FilterHelper.GetMinMaxDatesFromTimeRange("3M");
        Assert.Equal(today.AddMonths(-3), start);
        Assert.Equal(today, end);
    }

    [Theory]
    [InlineData("YTD")]
    [InlineData("ytd")] // case-insensitive
    public void GetMinMaxDatesFromTimeRange_YTD(string input)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (start, end) = FilterHelper.GetMinMaxDatesFromTimeRange(input);
        Assert.Equal(new DateOnly(today.Year, 1, 1), start);
        Assert.Equal(today, end);
    }

    [Theory]
    [InlineData("ALL")]
    [InlineData("unknown")]
    public void GetMinMaxDatesFromTimeRange_AllOrUnknown_DefaultsToMin_ToToday(string input)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (start, end) = FilterHelper.GetMinMaxDatesFromTimeRange(input);
        Assert.Equal(DateOnly.MinValue, start);
        Assert.Equal(today, end);
    }

    // -------- GetMinMaxDatesFromYear --------
    [Fact]
    public void GetMinMaxDatesFromYear_ReturnsJan1ToDec31()
    {
        var (start, end) = FilterHelper.GetMinMaxDatesFromYear(2024);
        Assert.Equal(new DateOnly(2024, 1, 1), start);
        Assert.Equal(new DateOnly(2024, 12, 31), end);
    }

    // -------- FilterLineChartDataPoints --------
    [Fact]
    public void FilterLineChartDataPoints_InclusiveRange_IgnoresInvalidLabels()
    {
        var points = new List<DataPointDto>
        {
            new DataPointDto { Label = "2025-01-01" },
            new DataPointDto { Label = "2025-01-02" },
            new DataPointDto { Label = "2025-01-03" },
            new DataPointDto { Label = "2025/01/04" }, // wrong format -> ignored
            new DataPointDto { Label = "not-a-date" }, // ignored
        };

        var start = new DateOnly(2025, 1, 1);
        var end = new DateOnly(2025, 1, 2);

        var result = FilterHelper.FilterLineChartDataPoints(points, start, end);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(
            DateTime.TryParseExact(p.Label, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d)
            && DateOnly.FromDateTime(d) >= start && DateOnly.FromDateTime(d) <= end
        ));
    }

    [Fact]
    public void FilterLineChartDataPoints_BoundariesAreInclusive()
    {
        var points = new List<DataPointDto>
        {
            new DataPointDto { Label = "2025-01-10" },
        };

        var start = new DateOnly(2025, 1, 10);
        var end = new DateOnly(2025, 1, 10);

        var result = FilterHelper.FilterLineChartDataPoints(points, start, end);

        Assert.Single(result);
        Assert.Equal("2025-01-10", result[0].Label);
    }

    // -------- TIMERANGES constant --------
    [Fact]
    public void Timeranges_AreExactlyAsSpecified()
    {
        var expected = new[] { "1W", "1M", "3M", "YTD", "ALL" };
        Assert.Equal(expected, FilterHelper.TIMERANGES);
    }
}
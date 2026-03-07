using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio.Application.Helpers;
using Kvandijk.Portfolio.Domain.Utils;

namespace Kvandijk.Portfolio.Tests.Application.Helpers;

public class PortfolioCalculationHelperTests
{
    // -------- NormalizeSeries --------

    [Fact]
    public void NormalizeSeries_SubtractsFirstValueFromAll()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 100m },
            new() { Label = "2025-01-02", Value = 150m },
            new() { Label = "2025-01-03", Value = 80m },
        };

        var result = PortfolioCalculationHelper.NormalizeSeries(points);

        Assert.Equal(0m, result[0].Value);
        Assert.Equal(50m, result[1].Value);
        Assert.Equal(-20m, result[2].Value);
    }

    [Fact]
    public void NormalizeSeries_EmptyList_ReturnsEmpty()
    {
        var result = PortfolioCalculationHelper.NormalizeSeries(new List<DataPointDto>());

        Assert.Empty(result);
    }

    [Fact]
    public void NormalizeSeries_SinglePoint_ReturnsZero()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 42m },
        };

        var result = PortfolioCalculationHelper.NormalizeSeries(points);

        Assert.Single(result);
        Assert.Equal(0m, result[0].Value);
    }

    [Fact]
    public void NormalizeSeries_PreservesLabels()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "day-one", Value = 10m },
            new() { Label = "day-two", Value = 20m },
        };

        var result = PortfolioCalculationHelper.NormalizeSeries(points);

        Assert.Equal("day-one", result[0].Label);
        Assert.Equal("day-two", result[1].Label);
    }

    // -------- GetPeriodDelta --------

    [Fact]
    public void GetPeriodDelta_ValueMode_ReturnsLastMinusFirst()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 100m },
            new() { Label = "2025-01-02", Value = 130m },
        };

        var result = PortfolioCalculationHelper.GetPeriodDelta(points, DashboardPresentationModes.Value);

        Assert.Equal(30m, result);
    }

    [Fact]
    public void GetPeriodDelta_ProfitMode_ReturnsLastValue()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 0m },
            new() { Label = "2025-01-02", Value = 50m },
        };

        var result = PortfolioCalculationHelper.GetPeriodDelta(points, DashboardPresentationModes.Profit);

        Assert.Equal(50m, result);
    }

    [Fact]
    public void GetPeriodDelta_ProfitPercentageMode_ReturnsLastValue()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 0m },
            new() { Label = "2025-01-02", Value = 12.5m },
        };

        var result = PortfolioCalculationHelper.GetPeriodDelta(points, DashboardPresentationModes.ProfitPercentage);

        Assert.Equal(12.5m, result);
    }

    [Fact]
    public void GetPeriodDelta_EmptyList_ReturnsNull()
    {
        var result = PortfolioCalculationHelper.GetPeriodDelta(new List<DataPointDto>(), DashboardPresentationModes.Value);

        Assert.Null(result);
    }

    [Fact]
    public void GetPeriodDelta_UnknownMode_ReturnsNull()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 100m },
        };

        var result = PortfolioCalculationHelper.GetPeriodDelta(points, "unknown-mode");

        Assert.Null(result);
    }

    [Fact]
    public void GetPeriodDelta_SinglePoint_ValueMode_ReturnsZero()
    {
        var points = new List<DataPointDto>
        {
            new() { Label = "2025-01-01", Value = 100m },
        };

        var result = PortfolioCalculationHelper.GetPeriodDelta(points, DashboardPresentationModes.Value);

        Assert.Equal(0m, result);
    }

    // -------- GetDashboardTableRows --------

    [Fact]
    public void GetDashboardTableRows_HappyPath_CalculatesCorrectly()
    {
        var tickers = new List<string> { "AAPL" };
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 60m },
        };

        var rows = PortfolioCalculationHelper.GetDashboardTableRows(tickers, transactions, history);

        Assert.Single(rows);
        Assert.Equal("AAPL", rows[0].Ticker);
        Assert.Equal(10m, rows[0].Amount);
        Assert.Equal(500m, rows[0].TotalInvestment); // 10 * 50
        Assert.Equal(600m, rows[0].Worth);            // 10 * 60
        Assert.Equal(100m, rows[0].Profit);            // 600 - 500
        Assert.Equal(0.2m, rows[0].ProfitPercentage);  // 100 / 500
        Assert.Equal(1m, rows[0].PortfolioPercentage); // only ticker
    }

    [Fact]
    public void GetDashboardTableRows_MultipleTickers_PortfolioPercentagesSumToOne()
    {
        var tickers = new List<string> { "AAPL", "MSFT" };
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Amount = 10m, PurchasePrice = 100m, TransactionCosts = 0m },
            new() { Ticker = "MSFT", Amount = 5m, PurchasePrice = 200m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 100m },
            new() { Ticker = "MSFT", Date = new DateOnly(2025, 1, 1), Close = 200m },
        };

        var rows = PortfolioCalculationHelper.GetDashboardTableRows(tickers, transactions, history);

        var totalPercentage = rows.Sum(r => r.PortfolioPercentage);
        Assert.Equal(1m, totalPercentage);
    }

    [Fact]
    public void GetDashboardTableRows_NoMarketData_WorthIsZero()
    {
        var tickers = new List<string> { "AAPL" };
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>();

        var rows = PortfolioCalculationHelper.GetDashboardTableRows(tickers, transactions, history);

        Assert.Single(rows);
        Assert.Equal(0m, rows[0].Worth);
    }

    [Fact]
    public void GetDashboardTableRows_ZeroInvestment_ProfitPercentageIsZero()
    {
        var tickers = new List<string> { "AAPL" };
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Amount = 0m, PurchasePrice = 0m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 100m },
        };

        var rows = PortfolioCalculationHelper.GetDashboardTableRows(tickers, transactions, history);

        Assert.Equal(0m, rows[0].ProfitPercentage);
    }

    [Fact]
    public void GetDashboardTableRows_ZeroTotalWorth_PortfolioPercentageIsZero()
    {
        var tickers = new List<string> { "AAPL" };
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>();

        var rows = PortfolioCalculationHelper.GetDashboardTableRows(tickers, transactions, history);

        Assert.Equal(0m, rows[0].PortfolioPercentage);
    }

    [Fact]
    public void GetDashboardTableRows_BuySellTransaction_CorrectNetPosition()
    {
        var tickers = new List<string> { "AAPL" };
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
            new() { Ticker = "AAPL", Amount = -3m, PurchasePrice = 60m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 70m },
        };

        var rows = PortfolioCalculationHelper.GetDashboardTableRows(tickers, transactions, history);

        Assert.Equal(7m, rows[0].Amount);         // 10 - 3
        Assert.Equal(490m, rows[0].Worth);          // 7 * 70
    }

    // -------- GetPortfolioLineChart --------

    [Fact]
    public void GetPortfolioLineChart_WorthMode_ReturnsPositionTimesPrice()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        Assert.Single(result.DataPoints);
        Assert.Equal(600m, result.DataPoints[0].Value); // 10 * 60
    }

    [Fact]
    public void GetPortfolioLineChart_ProfitMode_ReturnsWorthMinusInvested()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Profit", "currency", (worth, invested) => worth - invested);

        Assert.Single(result.DataPoints);
        Assert.Equal(100m, result.DataPoints[0].Value); // (10*60) - (10*50)
    }

    [Fact]
    public void GetPortfolioLineChart_ProfitPercentageMode_ReturnsPercentage()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Profit%", "percentage",
            (worth, invested) => invested != 0m ? (worth - invested) / invested * 100m : 0m);

        Assert.Single(result.DataPoints);
        Assert.Equal(20m, result.DataPoints[0].Value); // (600-500)/500 * 100
    }

    [Fact]
    public void GetPortfolioLineChart_ProfitPercentageMode_ZeroInvested_ReturnsZero()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 0m, PurchasePrice = 0m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Profit%", "percentage",
            (worth, invested) => invested != 0m ? (worth - invested) / invested * 100m : 0m);

        Assert.Single(result.DataPoints);
        Assert.Equal(0m, result.DataPoints[0].Value);
    }

    [Fact]
    public void GetPortfolioLineChart_TransactionsBeforeHistory_CaughtUpOnFirstDate()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 12, 1), Amount = 5m, PurchasePrice = 40m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 50m },
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 55m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        Assert.Equal(2, result.DataPoints.Count);
        Assert.Equal(250m, result.DataPoints[0].Value); // 5 * 50
        Assert.Equal(275m, result.DataPoints[1].Value); // 5 * 55
    }

    [Fact]
    public void GetPortfolioLineChart_MultipleTickers_SumsWorthCorrectly()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
            new() { Ticker = "MSFT", Date = new DateOnly(2025, 1, 1), Amount = 5m, PurchasePrice = 100m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
            new() { Ticker = "MSFT", Date = new DateOnly(2025, 1, 2), Close = 120m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        Assert.Single(result.DataPoints);
        Assert.Equal(1200m, result.DataPoints[0].Value); // (10*60) + (5*120)
    }

    [Fact]
    public void GetPortfolioLineChart_PriceForwardFill_UsesLastKnownPrice()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
            new() { Ticker = "MSFT", Date = new DateOnly(2025, 1, 1), Amount = 5m, PurchasePrice = 100m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
            new() { Ticker = "MSFT", Date = new DateOnly(2025, 1, 2), Close = 110m },
            // Day 3: only AAPL has a price, MSFT should forward-fill
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 3), Close = 65m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        // Day 2: (10*60) + (5*110) = 1150
        Assert.Equal(1150m, result.DataPoints[0].Value);
        // Day 3: (10*65) + (5*110) = 1200 (MSFT forward-fills to 110)
        Assert.Equal(1200m, result.DataPoints[1].Value);
    }

    [Fact]
    public void GetPortfolioLineChart_SellAllShares_ContributesZeroToWorth()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Amount = -10m, PurchasePrice = 60m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 3), Close = 70m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        Assert.Single(result.DataPoints);
        Assert.Equal(0m, result.DataPoints[0].Value); // 0 shares * 70
    }

    [Fact]
    public void GetPortfolioLineChart_NoTransactions_ReturnsEmptyPoints()
    {
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 100m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            new List<TransactionDto>(), history, "Worth", "currency", (worth, invested) => worth);

        // Points exist from history dates, but worth is 0 (no positions)
        Assert.Single(result.DataPoints);
        Assert.Equal(0m, result.DataPoints[0].Value);
    }

    [Fact]
    public void GetPortfolioLineChart_NoHistory_ReturnsEmptyPoints()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, new List<MarketHistoryDataPointDto>(), "Worth", "currency", (worth, invested) => worth);

        Assert.Empty(result.DataPoints);
    }

    [Fact]
    public void GetPortfolioLineChart_LabelsAreSortedChronologically()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 3), Close = 55m },
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 1), Close = 50m },
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 52m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        Assert.Equal("2025-01-01", result.DataPoints[0].Label);
        Assert.Equal("2025-01-02", result.DataPoints[1].Label);
        Assert.Equal("2025-01-03", result.DataPoints[2].Label);
    }

    [Fact]
    public void GetPortfolioLineChart_TickerCaseInsensitive()
    {
        var transactions = new List<TransactionDto>
        {
            new() { Ticker = "aapl", Date = new DateOnly(2025, 1, 1), Amount = 10m, PurchasePrice = 50m, TransactionCosts = 0m },
        };
        var history = new List<MarketHistoryDataPointDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2025, 1, 2), Close = 60m },
        };

        var result = PortfolioCalculationHelper.GetPortfolioLineChart(
            transactions, history, "Worth", "currency", (worth, invested) => worth);

        Assert.Single(result.DataPoints);
        Assert.Equal(600m, result.DataPoints[0].Value);
    }
}

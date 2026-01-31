using System.Diagnostics;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
using Dashboard.Application.Interfaces;
using Dashboard._Web.ViewModels;
using Dashboard.Domain.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Dashboard._Web.Controllers;

public class DashboardController : Controller
{
    private readonly IAzureTableService _azureTableService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DashboardController(IAzureTableService azureTableService, IServiceScopeFactory scopeFactory, ILogger<DashboardController> logger, IStringLocalizer<SharedResource> localizer)
    {
        _azureTableService = azureTableService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet("/")]
    public IActionResult Index() => View();

    [HttpGet("/dashboard/content")]
    public async Task<IActionResult> DashboardContent(
        [FromQuery] string? mode = DashboardPresentationModes.Profit,
        [FromQuery] string? tickers = null,
        [FromQuery] string? timerange = null,
        [FromQuery] int? year = null)
    {
        var sw = Stopwatch.StartNew();

        var msBeforeTransactions = sw.ElapsedMilliseconds;
        var transactions = await _azureTableService.GetTransactionsAsync();
        var msAfterTransactions = sw.ElapsedMilliseconds;

        // Named tx because tickers it already used as parameter name
        var tx = transactions
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        // Determine optimal period based on filters
        string period = year.HasValue
            ? PeriodHelper.GetPeriodFromYear(year.Value)
            : PeriodHelper.GetPeriodFromTimeRange(timerange);

        var msBeforeMarketHistory = sw.ElapsedMilliseconds;
        var marketHistoryDataPoints = await GetMarketHistoryDataPoints(tx, period);
        var msAfterMarketHistory = sw.ElapsedMilliseconds;

        var tableViewModel = GetDashboardTableRows(tx, transactions, marketHistoryDataPoints);

        var filteredTransactions = FilterHelper.FilterTransactions(transactions, tickers);

        LineChartViewModel lineChartViewModel = mode switch
        {
            DashboardPresentationModes.Value => GetPortfolioWorthLineChart(filteredTransactions, marketHistoryDataPoints),
            DashboardPresentationModes.Profit => GetPortfolioProfitLineChart(filteredTransactions, marketHistoryDataPoints),
            DashboardPresentationModes.ProfitPercentage => GetPortfolioProfitPercentageLineChart(filteredTransactions, marketHistoryDataPoints),
            _ => throw new InvalidOperationException("mode cannot be null")
        };

        // Apply time range or year filter to line chart
        DateOnly startDate, endDate;
        if (year.HasValue)
        {
            // When year is specified, show data for that entire year
            (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(year.Value);
        }
        else
        {
            // Use timerange filter when no year is specified
            (startDate, endDate) = FilterHelper.GetMinMaxDatesFromTimeRange(timerange ?? "ALL");
        }

        // Limit startDate to first transaction date to avoid empty charts
        var firstTransactionDate = filteredTransactions.Any() ? filteredTransactions.Min(t => t.Date) : DateOnly.MinValue;
        if (startDate < firstTransactionDate) startDate = firstTransactionDate;

        lineChartViewModel.DataPoints = FilterHelper.FilterLineChartDataPoints(lineChartViewModel.DataPoints, startDate, endDate);
        
        if (mode is DashboardPresentationModes.Profit or DashboardPresentationModes.ProfitPercentage)
        {
            lineChartViewModel.DataPoints = NormalizeSeries(lineChartViewModel.DataPoints).ToList();
        }
        
        lineChartViewModel.Profit = GetPeriodDelta(lineChartViewModel.DataPoints, mode);

        var viewModel = new DashboardViewModel
        {
            TableRows = tableViewModel,
            LineChart = lineChartViewModel,
            Years = transactions.Select(t => t.Date.Year).Distinct().OrderBy(y => y).ToArray()
        };

        sw.Stop();
        _logger.LogInformation("Timings: Transactions={Transactions}ms, MarketHistory={MarketHistory}ms, Other={Other}ms, Total={Total}ms",
            msAfterTransactions - msBeforeTransactions,
            msAfterMarketHistory - msBeforeMarketHistory,
            sw.ElapsedMilliseconds - (msAfterTransactions - msBeforeTransactions) - (msAfterMarketHistory - msBeforeMarketHistory),
            sw.ElapsedMilliseconds);

        return PartialView("_DashboardContent", viewModel);
    }

    private async Task<List<MarketHistoryDataPointDto>> GetMarketHistoryDataPoints(List<string> tickers, string period)
    {
        // Kick off all requests concurrently
        var fetchTasks = tickers.Select(ticker => GetMarketHistoryForTickerAsync(ticker, period)).ToArray();
        var results = await Task.WhenAll(fetchTasks);

        // Log failures (if any)
        var failed = results.Where(r => r.Error is not null).ToList();
        if (failed.Count > 0)
        {
            TempData["ErrorMessage"] = $"Failed to fetch market data for {failed.Count} ticker(s): {string.Join(", ", failed.Select(f => f.Ticker))}. Please try again later.";
            _logger.LogWarning("Some tickers failed: {Tickers}", string.Join(", ", failed.Select(f => f.Ticker)));
        }

        var allDataPoints = results
            .Where(r => r.Data is not null && r.Data.History.Any())
            .SelectMany(r =>
            {
                foreach (var point in r.Data!.History)
                {
                    point.Ticker = r.Ticker;
                }

                return r.Data!.History;
            })
            .ToList();

        return allDataPoints;
    }

    private async Task<(string Ticker, MarketHistoryResponseDto? Data, Exception? Error)> GetMarketHistoryForTickerAsync(string ticker, string period)
    {
        using var scope = _scopeFactory.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<ITickerApiService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DashboardController>>();

        try
        {
            logger.LogInformation("Fetching market history for ticker {Ticker} with period {Period}", ticker, period);
            var data = await api.GetMarketHistoryResponseAsync(ticker, period);
            logger.LogInformation("Fetched market history for ticker {Ticker} with {Count} data points", ticker, data?.History.Count ?? 0);
            return (ticker, data, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch market history for ticker {Ticker}: {Message}", ticker, ex.Message);
            return (ticker, null, ex);
        }
    }

    private List<DashboardTableRowViewModel> GetDashboardTableRows(List<string> tickers, List<TransactionDto> transactions, List<MarketHistoryDataPointDto> marketHistoryDataPoints)
    {
        var latestClose = marketHistoryDataPoints
            .GroupBy(p => p.Ticker!.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Date).First().Close
            );

        var aggregates = tickers.Select(ticker =>
        {
            var transactionsByTicker = transactions
                .Where(tr => string.Equals(tr.Ticker, ticker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var amount = transactionsByTicker.Sum(tr => tr.Amount);
            var investment = transactionsByTicker.Sum(tr => tr.TotalCosts);
            var currentPrice = latestClose.GetValueOrDefault(ticker.ToUpperInvariant(), 0m);
            var worth = currentPrice * amount;
            var profit = worth - investment;
            var profitPercentage = investment > 0 ? profit / investment : 0m;

            return new
            {
                Ticker = ticker,
                Amount = amount,
                Investment = investment,
                Worth = worth,
                Profit = profit,
                ProfitPercentage = profitPercentage
            };
        }).ToList();

        var totalWorth = aggregates.Sum(a => a.Worth);

        var rows = aggregates.Select(a =>
        {
            var portfolioPercentage = totalWorth > 0 ? a.Worth / totalWorth : 0m;

            return new DashboardTableRowViewModel
            {
                Ticker = a.Ticker,
                PortfolioPercentage = portfolioPercentage,
                Amount = a.Amount,
                TotalInvestment = a.Investment,
                Worth = a.Worth,
                Profit = a.Profit,
                ProfitPercentage = a.ProfitPercentage,
            };
        }).ToList();

        return rows;
    }

    private LineChartViewModel GetPortfolioWorthLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPointDto> history, string? title = null)
    {
        title ??= _localizer["PortfolioWorth"];
        return GetPortfolioLineChart(transactions, history, title, "currency", (worth, invested) => worth);
    }

    private LineChartViewModel GetPortfolioProfitLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPointDto> history, string? title = null)
    {
        title ??= _localizer["PortfolioProfitEur"];
        return GetPortfolioLineChart(transactions, history, title, "currency", (worth, invested) => worth - invested);
    }

    private LineChartViewModel GetPortfolioProfitPercentageLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPointDto> history, string? title = null)
    {
        title ??= _localizer["PortfolioProfitPct"];
        return GetPortfolioLineChart(transactions, history, title, "percentage", (worth, invested) => invested != 0m ? (worth - invested) / invested * 100m : 0m);
    }

    private LineChartViewModel GetPortfolioLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPointDto> history, string title, string format, Func<decimal, decimal, decimal> selector)
    {
        var transactionsByTicker = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Ticker))
            .GroupBy(t => t.Ticker.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

        var historyByTicker = history
            .Where(h => !string.IsNullOrWhiteSpace(h.Ticker))
            .GroupBy(h => h.Ticker!.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

        var allDates = historyByTicker.Values
            .SelectMany(g => g.Select(x => x.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var tickers = transactionsByTicker.Keys.Union(historyByTicker.Keys).ToHashSet();
        var positions = tickers.ToDictionary(t => t, _ => 0m);
        var txIndex = tickers.ToDictionary(t => t, _ => 0);
        var lastPrices = tickers.ToDictionary(t => t, _ => (decimal?)null);

        var priceMap = historyByTicker.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDictionary(x => x.Date, x => x.Close)
        );

        decimal netInvested = 0m;
        var points = new List<DataPointDto>(allDates.Count);

        foreach (var date in allDates)
        {
            // process transactions up to and including this date
            foreach (var t in tickers)
            {
                if (!transactionsByTicker.TryGetValue(t, out var txs)) continue;

                while (txIndex[t] < txs.Count && txs[txIndex[t]].Date <= date)
                {
                    var tx = txs[txIndex[t]++];
                    positions[t] += tx.Amount;
                    netInvested += tx.TotalCosts;
                }
            }

            // update last known prices
            foreach (var t in tickers)
            {
                if (priceMap.TryGetValue(t, out var pricePerDate) &&
                    pricePerDate.TryGetValue(date, out var price))
                {
                    lastPrices[t] = price;
                }
            }

            // total market worth at this date
            decimal totalWorth = tickers.Sum(t =>
                lastPrices[t] is decimal p && positions[t] != 0m ? positions[t] * p : 0m);

            // projection via selector
            decimal y = selector(totalWorth, netInvested);

            points.Add(new DataPointDto
            {
                Label = date.ToString("yyyy-MM-dd"),
                Value = y
            });
        }

        return new LineChartViewModel
        {
            Title = title,
            DataPoints = points,
            Format = format,
        };
    }

    /// <summary>
    /// Normalizes a series of data points by subtracting the value of the first point from each point in the series.
    /// This is used to show profit or profit percentage relative to the starting point.
    /// </summary>
    private static IReadOnlyList<DataPointDto> NormalizeSeries(IReadOnlyList<DataPointDto> points)
    {
        var first = points.FirstOrDefault()?.Value ?? 0m;
        return points.Select(p => new DataPointDto
        {
            Label = p.Label,
            Value = p.Value - first
        }).ToList();
    }

    /// <summary>
    /// Calculates the period delta based on the specified mode and a list of data points.
    /// </summary>
    private static decimal? GetPeriodDelta(IReadOnlyList<DataPointDto> points, string mode)
    {
        if (points.Count == 0) return null;

        var first = points[0].Value;
        var last = points[^1].Value;

        return mode switch
        {
            DashboardPresentationModes.Value => last - first,
            DashboardPresentationModes.Profit => last,
            DashboardPresentationModes.ProfitPercentage => last,
            _ => null
        };
    }
}

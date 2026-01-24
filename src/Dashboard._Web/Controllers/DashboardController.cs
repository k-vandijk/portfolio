using System.Diagnostics;
using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard._Web.ViewModels;
using Dashboard.Application.Helpers;
using kvandijk.Common.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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

    // TODO optimaliseren

    [HttpGet("/")]
    public IActionResult Index() => View();

    [SkipRequestTiming]
    [HttpGet("/dashboard/content")]
    public async Task<IActionResult> DashboardContent(
        [FromQuery] string? mode = "profit", // mode = value | profit | profit-percentage
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

        // TODO minder markthistorie ophalen voor performance
        var msBeforeMarketHistory = sw.ElapsedMilliseconds;
        var marketHistoryDataPoints = await GetMarketHistoryDataPoints(tx);
        var msAfterMarketHistory = sw.ElapsedMilliseconds;

        var tableViewModel = GetDashboardTableRows(tx, transactions, marketHistoryDataPoints);

        var filteredTransactions = FilterHelper.FilterTransactions(transactions, tickers);

        LineChartViewModel lineChartViewModel = mode switch
        {
            "value" => GetPortfolioWorthLineChart(filteredTransactions, marketHistoryDataPoints),
            "profit" => GetPortfolioProfitLineChart(filteredTransactions, marketHistoryDataPoints),
            "profit-percentage" => GetPortfolioProfitPercentageLineChart(filteredTransactions, marketHistoryDataPoints),
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
        lineChartViewModel.DataPoints = NormalizeSeries(lineChartViewModel.DataPoints, mode);
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

    [AllowAnonymous]
    [SkipRequestTiming]
    [HttpGet("/dashboard/lineonly.png")]
    public async Task<IActionResult> LineOnlyImage(
        [FromQuery] string? mode = "profit",
        [FromQuery] string? tickers = null,
        [FromQuery] string? timerange = null,
        [FromQuery] int? year = null,
        [FromQuery] int width = 1200,
        [FromQuery] int height = 400,
        [FromQuery] int padding = 16,
        [FromQuery] bool transparent = true)
    {
        width = Math.Clamp(width, 150, 4000);
        height = Math.Clamp(height, 60, 2000);
        padding = Math.Clamp(padding, 0, 200);

        var transactions = await _azureTableService.GetTransactionsAsync();

        var allTickers = transactions
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        var marketHistoryDataPoints = await GetMarketHistoryDataPoints(allTickers);

        var filteredTransactions = FilterHelper.FilterTransactions(transactions, tickers);

        LineChartViewModel chart = mode switch
        {
            "value" => GetPortfolioWorthLineChart(filteredTransactions, marketHistoryDataPoints),
            "profit" => GetPortfolioProfitLineChart(filteredTransactions, marketHistoryDataPoints),
            "profit-percentage" => GetPortfolioProfitPercentageLineChart(filteredTransactions, marketHistoryDataPoints),
            _ => throw new InvalidOperationException("Invalid mode")
        };

        DateOnly startDate, endDate;
        if (year.HasValue)
            (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(year.Value);
        else
            (startDate, endDate) = FilterHelper.GetMinMaxDatesFromTimeRange(timerange ?? "ALL");

        var firstTransactionDate = filteredTransactions.Any()
            ? filteredTransactions.Min(t => t.Date)
            : DateOnly.MinValue;

        if (startDate < firstTransactionDate) startDate = firstTransactionDate;

        chart.DataPoints = FilterHelper.FilterLineChartDataPoints(chart.DataPoints, startDate, endDate);
        chart.DataPoints = NormalizeSeries(chart.DataPoints, mode);

        var png = LineOnlyRenderer.RenderPng(
            chart.DataPoints,
            width,
            height,
            padding,
            transparentBackground: transparent);

        Response.Headers.CacheControl = "private, max-age=60";
        return File(png, "image/png");
    }

    private async Task<List<MarketHistoryDataPoint>> GetMarketHistoryDataPoints(List<string> tickers)
    {
        // Kick off all requests concurrently
        var fetchTasks = tickers.Select(GetMarketHistoryForTickerAsync).ToArray();
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

    private async Task<(string Ticker, MarketHistoryResponseDto? Data, Exception? Error)> GetMarketHistoryForTickerAsync(string ticker)
    {
        using var scope = _scopeFactory.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<ITickerApiService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DashboardController>>();

        try
        {
            logger.LogInformation("Fetching market history for ticker {Ticker}", ticker);



            var data = await api.GetMarketHistoryResponseAsync(ticker);
            logger.LogInformation("Fetched market history for ticker {Ticker} with {Count} data points", ticker, data?.History.Count ?? 0);
            return (ticker, data, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch market history for ticker {Ticker}: {Message}", ticker, ex.Message);
            return (ticker, null, ex);
        }
    }

    private List<DashboardTableRowViewModel> GetDashboardTableRows(List<string> tickers, List<TransactionDto> transactions, List<MarketHistoryDataPoint> marketHistoryDataPoints)
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
            var currentPrice = latestClose.TryGetValue(ticker.ToUpperInvariant(), out var p) ? p : 0m;
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

    private LineChartViewModel GetPortfolioWorthLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPoint> history, string? title = null)
    {
        title ??= _localizer["PortfolioWorth"];
        return GetPortfolioLineChart(transactions, history, title, "currency", (worth, invested) => worth);
    }

    private LineChartViewModel GetPortfolioProfitLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPoint> history, string? title = null)
    {
        title ??= _localizer["PortfolioProfitEur"];
        return GetPortfolioLineChart(transactions, history, title, "currency", (worth, invested) => worth - invested);
    }

    private LineChartViewModel GetPortfolioProfitPercentageLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPoint> history, string? title = null)
    {
        title ??= _localizer["PortfolioProfitPct"];
        return GetPortfolioLineChart(transactions, history, title, "percentage", (worth, invested) => invested != 0m ? (worth - invested) / invested * 100m : 0m);
    }

    private LineChartViewModel GetPortfolioLineChart(List<TransactionDto> transactions, List<MarketHistoryDataPoint> history, string title, string format, Func<decimal, decimal, decimal> selector)
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

    private static List<DataPointDto> NormalizeSeries(IReadOnlyList<DataPointDto> points, string? mode)
    {
        // If 'profit' or 'profit-percentage', normalize to start at zero

        if (mode == "profit" || mode == "profit-percentage")
        {
            var first = points.FirstOrDefault()?.Value ?? 0m;

            return points.Select(p => new DataPointDto
            {
                Label = p.Label,
                Value = p.Value - first
            }).ToList();
        }

        return points.ToList();
    }

    private static decimal? GetPeriodDelta(List<DataPointDto> points, string mode)
    {
        return points.Count > 0
            ? mode == "value"
                ? points[^1].Value - points[0].Value
                : points[^1].Value
            : null;
    }
}
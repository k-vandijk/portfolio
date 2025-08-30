using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Web.Helpers;
using Web.Models;
using Web.Services;
using Web.ViewModels;

namespace Web.Controllers;

public class DashboardController : Controller
{
    private readonly IAzureTableService _azureTableService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IAzureTableService azureTableService, IServiceScopeFactory scopeFactory, ILogger<DashboardController> logger)
    {
        _azureTableService = azureTableService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // TODO Fix line charts when filtered by ticker (and test filtered date range)
    [HttpGet("/dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] string? mode = null, // mode = value | profit | profit-percentage
        [FromQuery] string? tickers = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        var transactions = _azureTableService.GetTransactions();
        var filteredTransactions = TransactionsFilter.FilterTransactions(transactions, tickers, startDate, endDate);

        // Named tx because tickers it already used as parameter name
        var tx = filteredTransactions
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        var marketHistoryDataPoints = await GetMarketHistoryDataPoints(tx);

        var tableViewModel = GetDashboardTableRows(tx, transactions, marketHistoryDataPoints);

        LineChartViewModel lineChartViewModel = mode switch
        {
            "value" => GetPortfolioWorthLineChart(transactions, marketHistoryDataPoints),
            "profit" => GetPortfolioProfitLineChart(transactions, marketHistoryDataPoints),
            "profit-percentage" => GetPortfolioProfitPercentageLineChart(transactions, marketHistoryDataPoints),
            _ => GetPortfolioProfitLineChart(transactions, marketHistoryDataPoints), // Default to 'profit'
        };
        
        var viewModel = new DashboardViewModel
        {
            TableRows = tableViewModel,
            LineChart = lineChartViewModel
        };

        return View(viewModel);
    }

    private async Task<List<MarketHistoryDataPoint>> GetMarketHistoryDataPoints(List<string> tickers)
    {
        // Kick off all requests concurrently
        var fetchTasks = tickers.Select(GetMarketHistoryForTickerAsync).ToArray();
        var results = await Task.WhenAll(fetchTasks);

        // Log failures (if any)
        // TODO display this in ui
        var failed = results.Where(r => r.Error is not null).ToList();
        if (failed.Count > 0)
        {
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

    private async Task<(string Ticker, MarketHistoryResponse? Data, Exception? Error)> GetMarketHistoryForTickerAsync(string ticker)
    {
        using var scope = _scopeFactory.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<ITickerApiService>();

        using (_logger.BeginScope(new Dictionary<string, object> { ["Ticker"] = ticker }))
        {
            try
            {
                _logger.LogInformation("Fetching market history...");
                var data = await api.GetMarketHistoryResponseAsync(ticker);
                _logger.LogInformation("Fetched market history: {HasData}", data != null);
                return (ticker, data, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch market history");
                return (ticker, null, ex);
            }
        }
    }

    private List<DashboardTableRowViewModel> GetDashboardTableRows(List<string> tickers, List<Transaction> transactions, List<MarketHistoryDataPoint> marketHistoryDataPoints)
    {
        var latestClose = marketHistoryDataPoints
            .GroupBy(p => p.Ticker.ToUpperInvariant())
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

    private LineChartViewModel GetPortfolioWorthLineChart(List<Transaction> transactions, List<MarketHistoryDataPoint> history, string title = "Portfolio value €") =>
        GetPortfolioLineChart(transactions, history, title, "currency", (worth, invested) => worth);

    private LineChartViewModel GetPortfolioProfitLineChart(List<Transaction> transactions, List<MarketHistoryDataPoint> history, string title = "Portfolio profit €") =>
        GetPortfolioLineChart(transactions, history, title, "currency", (worth, invested) => worth - invested);

    private LineChartViewModel GetPortfolioProfitPercentageLineChart(List<Transaction> transactions, List<MarketHistoryDataPoint> history, string title = "Portfolio profit %") =>
        GetPortfolioLineChart(transactions, history, title, "percentage", (worth, invested) => invested != 0m ? (worth - invested) / invested * 100m : 0m);

    private LineChartViewModel GetPortfolioLineChart(List<Transaction> transactions, List<MarketHistoryDataPoint> history, string title, string format, Func<decimal, decimal, decimal> selector)
    {
        var transactionsByTicker = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Ticker))
            .GroupBy(t => t.Ticker.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

        var historyByTicker = history
            .Where(h => !string.IsNullOrWhiteSpace(h.Ticker))
            .GroupBy(h => h.Ticker.ToUpperInvariant())
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
        var points = new List<LineChartDataPoint>(allDates.Count);

        foreach (var date in allDates)
        {
            // transacties t/m datum verwerken
            foreach (var t in tickers)
            {
                if (!transactionsByTicker.TryGetValue(t, out var txs)) continue;

                while (txIndex[t] < txs.Count && txs[txIndex[t]].Date <= date)
                {
                    var tx = txs[txIndex[t]++];
                    positions[t] += tx.Amount;
                    netInvested += (tx.Amount * tx.PurchasePrice) + tx.TransactionCosts;
                }
            }

            // laatste bekende prijzen bijwerken
            foreach (var t in tickers)
            {
                if (priceMap.TryGetValue(t, out var pricePerDate) &&
                    pricePerDate.TryGetValue(date, out var price))
                {
                    lastPrices[t] = price;
                }
            }

            // totale marktwaarde
            decimal totalWorth = tickers.Sum(t =>
                lastPrices[t] is decimal p && positions[t] != 0m ? positions[t] * p : 0m);

            // projectie via selector
            decimal y = selector(totalWorth, netInvested);

            points.Add(new LineChartDataPoint
            {
                Label = date.ToString("yyyy-MM-dd", CultureInfo.GetCultureInfo("nl-NL")),
                Value = y
            });
        }

        return new LineChartViewModel
        {
            Title = title,
            DataPoints = points,
            Format = format
        };
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
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

    // TODO Add filters for tickers
    // TODO Add filters for dates
    // TODO Add switch for profit %

    [HttpGet("/dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var transactions = _azureTableService.GetTransactions();
        var tickers = transactions
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        var marketHistoryDataPoints = await GetMarketHistoryDataPoints(tickers);

        var tableViewModel = GetTableViewModel(tickers, transactions, marketHistoryDataPoints);
        var lineChartViewModel = GetPortfolioWorthLineChart(transactions, marketHistoryDataPoints);

        var viewModel = new DashboardViewModel
        {
            Table = tableViewModel,
            LineChart = lineChartViewModel
        };

        return View(viewModel);
    }

    private TableViewModel GetTableViewModel(List<string> tickers, List<Transaction> transactions, List<MarketHistoryDataPoint> marketHistoryDataPoints)
    {
        var ci = CultureInfo.GetCultureInfo("nl-NL");

        // Latest close per ticker
        var latestClose = marketHistoryDataPoints
            .GroupBy(p => p.Ticker.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Date).First().Close
            );

        // Aggregate per ticker
        var aggregates = tickers.Select(ticker =>
        {
            var txs = transactions.Where(tr => string.Equals(tr.Ticker, ticker, StringComparison.OrdinalIgnoreCase));
            var amount = txs.Sum(tr => tr.Amount);
            var totalInvestment = txs.Sum(tr => tr.TotalCosts);
            var currentPrice = latestClose.TryGetValue(ticker.ToUpperInvariant(), out var p) ? p : 0m;
            var worth = currentPrice * amount;
            var profitEur = worth - totalInvestment;
            var profitPct = totalInvestment > 0 ? profitEur / totalInvestment : 0m;

            return new
            {
                Ticker = ticker,
                Amount = amount,
                TotalInvestment = totalInvestment,
                Worth = worth,
                ProfitEur = profitEur,
                ProfitPct = profitPct
            };
        }).ToList();

        var totalWorth = aggregates.Sum(a => a.Worth);
        // Build rows (formatted strings for display)
        var rows = aggregates.Select(a =>
        {
            var portfolioPct = totalWorth > 0 ? a.Worth / totalWorth : 0m;

            return new Dictionary<string, object?>
            {
                ["Ticker"] = a.Ticker,
                ["Portfolio %"] = portfolioPct.ToString("P2", ci),
                ["Amount"] = a.Amount.ToString("N8", ci),
                ["Total investment"] = a.TotalInvestment.ToString("C2", ci),
                ["Worth"] = a.Worth.ToString("C2", ci),
                ["Profit €"] = a.ProfitEur.ToString("C2", ci),
                ["Profit %"] = a.ProfitPct.ToString("P2", ci),
            };
        }).Cast<IDictionary<string, object?>>().ToList();

        return new TableViewModel
        {
            Columns = new List<TableColumn>
            {
                new() { Header = "Ticker", Key = "Ticker" },
                new() { Header = "Portfolio %", Key = "Portfolio %" },
                new() { Header = "Amount", Key = "Amount" },
                new() { Header = "Total investment", Key = "Total investment" },
                new() { Header = "Worth", Key = "Worth" },
                new() { Header = "Profit €", Key = "Profit €" },
                new() { Header = "Profit %", Key = "Profit %" },
            },
            Rows = rows,
            EmptyText = "No data"
        };
    }

    private LineChartViewModel GetPortfolioWorthLineChart(List<Transaction> transactions, List<MarketHistoryDataPoint> history, string title = "Portfolio value €")
    {
        var ci = CultureInfo.GetCultureInfo("nl-NL");

        // Group inputs
        var transByTicker = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Ticker))
            .GroupBy(t => t.Ticker.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

        var histByTicker = history
            .Where(h => !string.IsNullOrWhiteSpace(h.Ticker))
            .GroupBy(h => h.Ticker.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

        // All dates we have prices for (union across tickers)
        var allDates = histByTicker.Values
            .SelectMany(g => g.Select(x => x.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var tickers = transByTicker.Keys
            .Union(histByTicker.Keys)
            .ToHashSet();

        // Per-ticker rolling state
        var pos = tickers.ToDictionary(t => t, _ => 0m);                       // cumulative Amount
        var txIdx = tickers.ToDictionary(t => t, _ => 0);                       // pointer into transactions
        var lastPrice = tickers.ToDictionary(t => t, _ => (decimal?)null);      // forward-filled price
        var priceAtDate = histByTicker.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDictionary(x => x.Date, x => x.Close)            // exact-date lookup
        );

        var points = new List<LineChartDataPoint>(capacity: allDates.Count);

        foreach (var date in allDates)
        {
            // 1) Apply all transactions up to and including 'date'
            foreach (var t in tickers)
            {
                if (!transByTicker.TryGetValue(t, out var txList)) continue;

                var i = txIdx[t];
                while (i < txList.Count && txList[i].Date <= date)
                {
                    // Amount can be negative for sells; this will reduce the position
                    pos[t] += txList[i].Amount;
                    i++;
                }
                txIdx[t] = i;
            }

            // 2) Refresh last known price for this date (forward-fill)
            foreach (var t in tickers)
            {
                if (priceAtDate.TryGetValue(t, out var map) && map.TryGetValue(date, out var close))
                {
                    lastPrice[t] = close;
                }
            }

            // 3) Compute portfolio worth = Σ (position * lastPrice) across tickers
            decimal worth = 0m;
            foreach (var t in tickers)
            {
                var p = lastPrice[t];
                if (p.HasValue && pos[t] != 0m)
                    worth += pos[t] * p.Value;
            }

            points.Add(new LineChartDataPoint
            {
                Label = date.ToString("yyyy-MM-dd", ci),
                Value = worth
            });
        }

        return new LineChartViewModel
        {
            Title = title,
            DataPoints = points,
            Format = "currency"
        };
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
}
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
    private readonly ITransactionService _azureTableService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public DashboardController(ITransactionService azureTableService, IServiceScopeFactory scopeFactory, ILogger<DashboardController> logger, IStringLocalizer<SharedResource> localizer)
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

        var tableViewModel = PortfolioCalculationHelper.GetDashboardTableRows(tx, transactions, marketHistoryDataPoints);

        var filteredTransactions = FilterHelper.FilterTransactions(transactions, tickers);

        LineChartDto lineChartDto = mode switch
        {
            DashboardPresentationModes.Value => PortfolioCalculationHelper.GetPortfolioLineChart(filteredTransactions, marketHistoryDataPoints, _localizer["PortfolioWorth"], "currency", (worth, invested) => worth),
            DashboardPresentationModes.Profit => PortfolioCalculationHelper.GetPortfolioLineChart(filteredTransactions, marketHistoryDataPoints, _localizer["PortfolioProfitEur"], "currency", (worth, invested) => worth - invested),
            DashboardPresentationModes.ProfitPercentage => PortfolioCalculationHelper.GetPortfolioLineChart(filteredTransactions, marketHistoryDataPoints, _localizer["PortfolioProfitPct"], "percentage", (worth, invested) => invested != 0m ? (worth - invested) / invested * 100m : 0m),
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

        lineChartDto.DataPoints = FilterHelper.FilterLineChartDataPoints(lineChartDto.DataPoints, startDate, endDate);

        if (mode is DashboardPresentationModes.Profit or DashboardPresentationModes.ProfitPercentage)
        {
            lineChartDto.DataPoints = PortfolioCalculationHelper.NormalizeSeries(lineChartDto.DataPoints).ToList();
        }

        lineChartDto.Profit = PortfolioCalculationHelper.GetPeriodDelta(lineChartDto.DataPoints, mode);

        var viewModel = new DashboardViewModel
        {
            TableRows = tableViewModel,
            LineChart = lineChartDto,
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
            _logger.LogWarning("Some tickers failed: {Tickers}", string.Join(", ", failed.Select(f => f.Ticker)));

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
}

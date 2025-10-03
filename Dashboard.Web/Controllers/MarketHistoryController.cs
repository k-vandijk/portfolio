using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace Dashboard.Web.Controllers;

public class MarketHistoryController : Controller
{
    private readonly ILogger<MarketHistoryController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MarketHistoryController(ILogger<MarketHistoryController> logger, IServiceScopeFactory scopeFactory, IStringLocalizer<SharedResource> localizer)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _localizer = localizer;
    }

    [HttpGet("/market-history")]
    public IActionResult Index() => View();

    [HttpGet("/market-history/content")]
    public async Task<IActionResult> MarketHistoryContent([FromQuery] string ticker)
    {
        var sw = Stopwatch.StartNew();

        var marketHistoryDataTask = GetMarketHistoryResponseAsync(ticker);
        var transactionsTask = GetTransactionsAsync();

        await Task.WhenAll(marketHistoryDataTask, transactionsTask);

        var marketHistoryData = marketHistoryDataTask.Result;
        var transactions = transactionsTask.Result;

        if (marketHistoryData == null) return NotFound();

        var lineChartViewModel = GetMarketHistoryViewModel(marketHistoryData);

        // from
        var first = lineChartViewModel.DataPoints.First();
        // to
        var last = lineChartViewModel.DataPoints.Last();

        decimal currentPrice = last.Value;
        string currentPriceString = currentPrice.ToString("C2");

        decimal interest = last.Value - first.Value;
        string interestString = interest.ToString("C2");

        decimal interestPercentage = interest / first.Value * 100;
        string interestPercentageString = interestPercentage.ToString("F2") + "%";

        var viewModel = new MarketHistoryViewModel
        {
            LineChart = lineChartViewModel,
            CurrentPriceString = currentPriceString,
            InterestString = interestString,
            InterestPercentageString = interestPercentageString,
            Tickers = transactions.Select(t => t.Ticker).Distinct().OrderBy(t => t).ToArray(),
        };

        sw.Stop();
        _logger.LogInformation("MarketHistory view rendered in {Elapsed} ms", sw.ElapsedMilliseconds);
        return PartialView("_MarketHistoryContent", viewModel);
    }

    private async Task<List<Transaction>> GetTransactionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAzureTableService>();

        return await service.GetTransactionsAsync();
    }

    private async Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(string ticker)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITickerApiService>();

        return await service.GetMarketHistoryResponseAsync(ticker);
    }

    private LineChartViewModel GetMarketHistoryViewModel(MarketHistoryResponse marketHistory)
    {
        var viewModel = new LineChartViewModel
        {
            Title = _localizer["MarketHistory"],
            DataPoints = marketHistory.History
                .OrderBy(h => h.Date) // Order so that we can find the latest value easily
                .Select(h => new DataPointDto
                {
                    Label = h.Date.ToString("dd-MM-yyyy"),
                    Value = h.Close
                }).ToList()
        };

        return viewModel;
    }
}
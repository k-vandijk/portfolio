using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;
using Dashboard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Dashboard.Web.Controllers;

public class MarketHistoryController : Controller
{
    private readonly ITickerApiService _service;
    private readonly ILogger<MarketHistoryController> _logger;
    private readonly IConfiguration _config;

    public MarketHistoryController(ITickerApiService service, ILogger<MarketHistoryController> logger, IConfiguration config)
    {
        _service = service;
        _logger = logger;
        _config = config;
    }

    [HttpGet("/market-history")]
    public IActionResult MarketHistory([FromQuery] string? ticker = "SXRV.DE")
    {
        return View();
    }

    [HttpGet("/market-history/section")]
    public async Task<IActionResult> MarketHistorySection([FromQuery] string ticker)
    {
        var sw = Stopwatch.StartNew();

        var tickerApiUrl = _config["Secrets:TickerApiurl"]
            ?? throw new ArgumentNullException("Secrets:TickerApiurl", "Please set the TickerApiurl in the configuration.");

        var tickerApiCode = _config["Secrets:TickerApiCode"]
            ?? throw new ArgumentNullException("Secrets:TickerApiCode", "Please set the TickerApiCode in the configuration.");

        var marketHistoryData = await _service.GetMarketHistoryResponseAsync(tickerApiUrl, tickerApiCode, ticker);
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
            InterestPercentageString = interestPercentageString
        };

        sw.Stop();
        _logger.LogInformation("MarketHistory view rendered in {Elapsed} ms", sw.ElapsedMilliseconds);
        return PartialView("_MarketHistorySection", viewModel);
    }

    private LineChartViewModel GetMarketHistoryViewModel(MarketHistoryResponse marketHistory)
    {
        var viewModel = new LineChartViewModel
        {
            Title = "Market history",
            DataPoints = marketHistory.History
                .OrderBy(h => h.Date) // Order so that we can find the latest value easily
                .Select(h => new LineChartDataPointDto
                {
                    Label = h.Date.ToString("dd-MM-yyyy"),
                    Value = h.Close
                }).ToList()
        };

        return viewModel;
    }
}
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Services;
using Web.ViewModels;

namespace Web.Controllers;

public class MarketHistoryController : Controller
{
    private readonly ITickerApiService _service;

    public MarketHistoryController(ITickerApiService service)
    {
        _service = service;
    }

    [HttpGet("/market-history")]
    public async Task<IActionResult> MarketHistory(
        [FromQuery] string? ticker = "SXRV.DE",
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        var marketHistoryData = await _service.GetMarketHistoryResponseAsync(ticker);
        if (marketHistoryData == null)
        {
            return NotFound();
        }

        var lineChartViewModel = GetMarketHistoryViewModel(marketHistoryData);

        LineChartDataPoint first = lineChartViewModel.DataPoints.First();
        LineChartDataPoint last = lineChartViewModel.DataPoints.Last();

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

        return View(viewModel);
    }

    private LineChartViewModel GetMarketHistoryViewModel(MarketHistoryResponse marketHistory)
    {
        var viewModel = new LineChartViewModel
        {
            Title = "Market history",
            DataPoints = marketHistory.History
                .OrderBy(h => h.Date) // Order so that we can find the latest value easily
                .Select(h => new LineChartDataPoint
                {
                    Label = h.Date.ToString("dd-MM-yyyy"),
                    Value = h.Close
                }).ToList()
        };

        return viewModel;
    }
}
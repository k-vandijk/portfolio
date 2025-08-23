using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web.Responses;
using Web.ViewModels;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public HomeController(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    [HttpGet("/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet("/investment")]
    public IActionResult Investment()
    {
        return View();
    }

    [HttpGet("/market-history")]
    public async Task<IActionResult> MarketHistory()
    {
        var viewModel = await GetMarketHistoryViewModelAsync("SXRV.DE");

        return View(viewModel);
    }

    public async Task<MarketHistoryResponse> GetMarketHistoryResponseAsync(string ticker)
    {
        var tickerApiUrl = _config["ticker-api-url"] ?? throw new InvalidOperationException("Ticker API URL is not configured.");
        var tickerApiCode = _config["ticker-api-code"] ?? throw new InvalidOperationException("Ticker API code is not configured.");
        var requestUrl = $"{tickerApiUrl}/get_history?code={tickerApiCode}&ticker={ticker}&period=2y&interval=1d";

        var response = await _http.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var marketHistory = System.Text.Json.JsonSerializer.Deserialize<MarketHistoryResponse>(responseContent, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return marketHistory;
    }

    public async Task<LineChartViewModel> GetMarketHistoryViewModelAsync(string ticker)
    {
        var marketHistory = await GetMarketHistoryResponseAsync(ticker);

        var viewModel = new LineChartViewModel
        {
            Title = "Market history",
            DataPoints = marketHistory.History
                .OrderBy(h => h.Date) // Order so that we can find the latest value easily
                .Select(h => new LineChartDataPoint
                {
                    Label = h.Date.ToString("dd.MM.yyyy"),
                    Value = h.Close
                }).ToList()
        };

        return viewModel;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
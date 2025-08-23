using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;
using Web.Responses;
using Web.ViewModels;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    public HomeController(HttpClient http, IConfiguration config, IMemoryCache cache)
    {
        _http = http;
        _config = config;
        _cache = cache;
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
    public async Task<IActionResult> MarketHistory([FromQuery] string? ticker = "SXRV.DE")
    {
        var marketHistoryData = await GetMarketHistoryResponseAsync(ticker);
        if (marketHistoryData == null)
        {
            return NotFound();
        }

        var viewModel = GetMarketHistoryViewModel(marketHistoryData);

        return View(viewModel);
    }

    // TODO Seperate caching logic from this method
    private async Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(
        string ticker, 
        string? period = "1y", 
        string? interval = "1d")
    {
        var cacheKey = $"market-history:{ticker}:{period}:{interval}";
        if (_cache.TryGetValue(cacheKey, out MarketHistoryResponse cached))
        {
            return cached;
        }

        var tickerApiUrl = _config["ticker-api-url"] ?? throw new InvalidOperationException("ticker-api-url is not configured.");
        var tickerApiCode = _config["ticker-api-code"] ?? throw new InvalidOperationException("ticker-api-code is not configured.");
        var requestUrl = $"{tickerApiUrl}/get_history?code={tickerApiCode}&ticker={ticker}&period={period}&interval={interval}";

        var response = await _http.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var marketHistory = JsonSerializer.Deserialize<MarketHistoryResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (marketHistory is not null)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(60)) // TODO Set to appropriate duration
                .SetSlidingExpiration(TimeSpan.FromMinutes(60)); // TODO Set to appropriate duration

            _cache.Set(cacheKey, marketHistory, options);
        }

        return marketHistory;
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
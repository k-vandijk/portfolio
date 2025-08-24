using System.Text.Json;
using Web.Models;

namespace Web.Services;

public interface ITickerApiService
{
    Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(string ticker, string? period = "1y", string? interval = "1d");
}

public class TickerApiService : ITickerApiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public TickerApiService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _http = httpFactory.CreateClient("cached-http-client");
        _config = config;
    }

    public async Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(
        string ticker,
        string? period = "1y",
        string? interval = "1d")
    {
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

        return marketHistory;
    }
}

using System.Text.Json;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;

namespace Dashboard.Infrastructure.Services;

public class TickerApiService : ITickerApiService
{
    private readonly HttpClient _http;

    public TickerApiService(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient("cached-http-client");
    }

    public async Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(
        string tickerApiUrl, 
        string tickerApiCode,
        string ticker,
        string? period = "1y",
        string? interval = "1d")
    {
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

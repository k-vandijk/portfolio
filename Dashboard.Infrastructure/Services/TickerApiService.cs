using System.Text.Json;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;

namespace Dashboard.Infrastructure.Services;

public class TickerApiService : ITickerApiService
{
    private readonly HttpClient _http;

    private readonly string FIRST_TRANSACTION_DATE = "2024-06-06";

    public TickerApiService(IHttpClientFactory httpFactory)
    {
        _http = httpFactory.CreateClient("cached-http-client");
    }

    public async Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(
        string tickerApiUrl, 
        string tickerApiCode,
        string ticker,
        string? period = null,
        string? interval = "1d")
    {
        period ??= GetPeriod();

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

    private string GetPeriod(DateOnly? firstTransactionDate = null)
    {
        firstTransactionDate ??= DateOnly.Parse(FIRST_TRANSACTION_DATE);

        // Get the difference in months between the first transaction date and today in years and add 1
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearsDifference = today.Year - firstTransactionDate.Value.Year;
        return $"{yearsDifference + 1}y";
    }
}

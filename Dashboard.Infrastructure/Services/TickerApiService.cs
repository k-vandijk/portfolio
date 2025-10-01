using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Dashboard.Domain.Utils;

namespace Dashboard.Infrastructure.Services;

public class TickerApiService : ITickerApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public TickerApiService(IHttpClientFactory httpFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpFactory;
        _cache = cache;
    }

    public async Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(
        string tickerApiUrl, 
        string tickerApiCode,
        string ticker,
        string? period = null,
        string? interval = "1d")
    {
        period ??= GetPeriod();

        var cacheKey = $"history:{ticker}:{period}:{interval}";
        if (_cache.TryGetValue(cacheKey, out MarketHistoryResponse? cached))
            return cached;

        var requestUrl = $"{tickerApiUrl}/get_history?code={tickerApiCode}&ticker={ticker}&period={period}&interval={interval}";
        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var marketHistory = JsonSerializer.Deserialize<MarketHistoryResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // 10 minuten sliding + 60 minuten absolute (voorbeeld)
        _cache.Set(cacheKey, marketHistory, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteExpirationMinutes)
        });

        return marketHistory;
    }

    private string GetPeriod(DateOnly? firstTransactionDate = null)
    {
        firstTransactionDate ??= DateOnly.Parse(StaticDetails.FirstTransactionDate);

        // Get the difference in months between the first transaction date and today in years and add 1
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearsDifference = today.Year - firstTransactionDate.Value.Year;
        return $"{yearsDifference + 1}y";
    }
}

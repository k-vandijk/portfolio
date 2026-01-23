using Dashboard.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
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
        string ticker,
        string? period = null,
        string? interval = "1d")
    {
        var tickerApiUrl = Environment.GetEnvironmentVariable("TICKER_API_URL")!;
        var tickerApiCode = Environment.GetEnvironmentVariable("TICKER_API_CODE")!;

        period ??= PeriodHelper.GetDefaultPeriod();

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
}

using System.Text.Json;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
using Dashboard.Application.HttpClientInterfaces;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Dashboard.Infrastructure.HttpClients;

public class TickerApiClient : ITickerApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public TickerApiClient(IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _config = config;
    }

    public async Task<MarketHistoryResponseDto?> GetMarketHistoryResponseAsync(
        string ticker,
        string? period = null,
        string? interval = "1d")
    {
        var tickerApiUrl = _config["TickerApi:Url"];
        var tickerApiCode = _config["TickerApi:Code"];

        period ??= PeriodHelper.GetDefaultPeriod();

        var cacheKey = $"history:{ticker}:{period}:{interval}";
        if (_cache.TryGetValue(cacheKey, out MarketHistoryResponseDto? cached))
            return cached;

        var requestUrl = $"{tickerApiUrl}/get_history?code={tickerApiCode}&ticker={ticker}&period={period}&interval={interval}";
        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(requestUrl);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var marketHistory = JsonSerializer.Deserialize<MarketHistoryResponseDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _cache.Set(cacheKey, marketHistory, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(StaticDetails.SlidingCacheExpirationMinutes),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(StaticDetails.AbsoluteCacheExpirationMinutes)
        });

        return marketHistory;
    }
}

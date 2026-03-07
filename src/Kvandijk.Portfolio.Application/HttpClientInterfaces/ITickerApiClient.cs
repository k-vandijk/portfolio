using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio.Application.HttpClientInterfaces;

public interface ITickerApiClient
{
    Task<MarketHistoryResponseDto?> GetMarketHistoryResponseAsync(string ticker, string? period = null, string? interval = "1d");
}

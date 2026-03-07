using Dashboard.Application.Dtos;

namespace Dashboard.Application.HttpClientInterfaces;

public interface ITickerApiClient
{
    Task<MarketHistoryResponseDto?> GetMarketHistoryResponseAsync(string ticker, string? period = null, string? interval = "1d");
}

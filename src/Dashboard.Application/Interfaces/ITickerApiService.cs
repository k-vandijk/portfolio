using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface ITickerApiService
{
    Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(string ticker, string? period = null, string? interval = "1d");
}
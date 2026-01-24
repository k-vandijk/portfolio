using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface ITickerApiService
{
    Task<MarketHistoryResponseDto?> GetMarketHistoryResponseAsync(string ticker, string? period = null, string? interval = "1d");
}
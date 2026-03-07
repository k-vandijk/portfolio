using Dashboard.Application.Dtos;

namespace Dashboard.Application.ServiceInterfaces;

public interface ITickerApiService
{
    Task<MarketHistoryResponseDto?> GetMarketHistoryResponseAsync(string ticker, string? period = null, string? interval = "1d");
}
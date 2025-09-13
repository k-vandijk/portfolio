using Dashboard.Domain.Models;

namespace Dashboard.Application.Interfaces;

public interface ITickerApiService
{
    Task<MarketHistoryResponse?> GetMarketHistoryResponseAsync(string tickerApiUrl, string tickerApiCode, string ticker, string? period = "1y", string? interval = "1d");
}
using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Services;

public class PortfolioValueService : IPortfolioValueService
{
    private readonly IAzureTableService _azureTableService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioValueService> _logger;

    public PortfolioValueService(
        IAzureTableService azureTableService,
        IServiceScopeFactory scopeFactory,
        ILogger<PortfolioValueService> logger)
    {
        _azureTableService = azureTableService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<HoldingInfo>> GetTopHoldingsByValueAsync(int count = 3)
    {
        var transactions = await _azureTableService.GetTransactionsAsync();

        var tickers = transactions
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        // Fetch latest close prices concurrently
        var fetchTasks = tickers.Select(async ticker =>
        {
            using var scope = _scopeFactory.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ITickerApiService>();

            try
            {
                var data = await api.GetMarketHistoryResponseAsync(ticker);
                var ordered = data?.History.OrderByDescending(h => h.Date).ToList();
                var latestClose = ordered?.FirstOrDefault()?.Close ?? 0m;
                var prevDayClose = ordered?.Skip(1).FirstOrDefault()?.Close ?? latestClose;
                return (Ticker: ticker, Price: latestClose, PrevClose: prevDayClose);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch price for {Ticker}", ticker);
                return (Ticker: ticker, Price: 0m, PrevClose: 0m);
            }
        }).ToArray();

        var prices = await Task.WhenAll(fetchTasks);
        var priceMap = prices.ToDictionary(p => p.Ticker, p => (p.Price, p.PrevClose));

        // Calculate total quantity per ticker
        var holdings = transactions
            .GroupBy(t => t.Ticker.ToUpperInvariant())
            .Select(g =>
            {
                var ticker = g.Key;
                var quantity = g.Sum(t => t.Amount);
                var (currentPrice, prevClose) = priceMap.GetValueOrDefault(ticker, (0m, 0m));
                var totalValue = quantity * currentPrice;
                return new HoldingInfo(ticker, quantity, currentPrice, totalValue, prevClose);
            })
            .Where(h => h.Quantity > 0) // Only active holdings
            .OrderByDescending(h => h.TotalValue)
            .Take(count)
            .ToList();

        return holdings;
    }
}

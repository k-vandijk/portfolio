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

    public async Task<decimal> GetCurrentPortfolioWorthAsync()
    {
        var transactions = await _azureTableService.GetTransactionsAsync();

        var tickers = transactions
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        // Fetch latest close prices concurrently (same pattern as DashboardController)
        var fetchTasks = tickers.Select(async ticker =>
        {
            using var scope = _scopeFactory.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ITickerApiService>();

            try
            {
                var data = await api.GetMarketHistoryResponseAsync(ticker);
                var latestClose = data?.History
                    .OrderByDescending(h => h.Date)
                    .FirstOrDefault()?.Close ?? 0m;
                return (Ticker: ticker, Price: latestClose);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch price for {Ticker}", ticker);
                return (Ticker: ticker, Price: 0m);
            }
        }).ToArray();

        var prices = await Task.WhenAll(fetchTasks);
        var priceMap = prices.ToDictionary(p => p.Ticker, p => p.Price);

        var totalWorth = transactions.Sum(t =>
        {
            var price = priceMap.GetValueOrDefault(t.Ticker.ToUpperInvariant(), 0m);
            return t.Amount * price;
        });

        return totalWorth;
    }
}

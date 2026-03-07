using Dashboard.Application.Dtos;
using Dashboard.Application.HttpClientInterfaces;
using Dashboard.Application.Mappers;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Application.ServiceInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Services;

public class PortfolioValueService : IPortfolioValueService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioValueService> _logger;
    private readonly ITransactionsRepository _transactionsRepository;

    public PortfolioValueService(
        IServiceScopeFactory scopeFactory,
        ILogger<PortfolioValueService> logger, 
        ITransactionsRepository transactionsRepository)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _transactionsRepository = transactionsRepository;
    }

    public async Task<List<HoldingInfo>> GetTopHoldingsByValueAsync(int count = 3)
    {
        var all = await GetAllHoldingsAsync();
        return all.Take(count).ToList();
    }

    public async Task<List<HoldingInfo>> GetAllHoldingsAsync()
    {
        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactionDtos = transactionEntities.Select(e => e.ToModel()).ToList();

        var tickers = transactionDtos
            .Select(t => t.Ticker.ToUpperInvariant())
            .Distinct()
            .ToList();

        // Fetch latest close prices concurrently
        var fetchTasks = tickers.Select(async ticker =>
        {
            using var scope = _scopeFactory.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ITickerApiClient>();

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

        var holdings = transactionDtos
            .GroupBy(t => t.Ticker.ToUpperInvariant())
            .Select(g =>
            {
                var ticker = g.Key;
                var quantity = g.Sum(t => t.Amount);
                var (currentPrice, prevClose) = priceMap.GetValueOrDefault(ticker, (0m, 0m));
                var totalValue = quantity * currentPrice;
                return new HoldingInfo(ticker, quantity, currentPrice, totalValue, prevClose);
            })
            .Where(h => h.Quantity > 0)
            .OrderByDescending(h => h.TotalValue)
            .ToList();

        return holdings;
    }
}

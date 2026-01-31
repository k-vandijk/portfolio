using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;

namespace Dashboard.Infrastructure.Services;

public class DummyTransactionService : ITransactionService
{
    private static readonly IComparer<TransactionDto> DateComparer = 
        Comparer<TransactionDto>.Create((a, b) => a.Date.CompareTo(b.Date));

    // Shared static data across all instances (generated once, shared by all scopes)
    private static readonly List<TransactionDto> SharedTransactions = GenerateDcaTransactions()
        .OrderBy(t => t.Date)
        .ToList();
    
    private static readonly object SharedLock = new();

    public Task<List<TransactionDto>> GetTransactionsAsync()
    {
        lock (SharedLock)
        {
            // Return a copy to prevent external modifications
            return Task.FromResult(SharedTransactions.ToList());
        }
    }

    public Task AddTransactionAsync(TransactionDto transaction)
    {
        // Generate RowKey if not present
        if (string.IsNullOrWhiteSpace(transaction.RowKey))
        {
            transaction.RowKey = Guid.NewGuid().ToString();
        }

        lock (SharedLock)
        {
            // Insert transaction in sorted order (by date)
            var index = SharedTransactions.BinarySearch(transaction, DateComparer);
            if (index < 0)
            {
                index = ~index; // BinarySearch returns bitwise complement of insert position when not found
            }
            SharedTransactions.Insert(index, transaction);
        }
        
        return Task.CompletedTask;
    }

    public Task DeleteTransactionAsync(string rowKey)
    {
        lock (SharedLock)
        {
            var transaction = SharedTransactions.FirstOrDefault(t => t.RowKey == rowKey);
            if (transaction != null)
            {
                SharedTransactions.Remove(transaction);
            }
        }
        
        return Task.CompletedTask;
    }

    private static List<TransactionDto> GenerateDcaTransactions()
    {
        var transactions = new List<TransactionDto>();
        var random = new Random(42); // Fixed seed for consistent data

        // Define DCA portfolios with different start dates
        var dcaStrategies = new[]
        {
            new { Ticker = "AAPL", MonthlyAmount = 500m, StartDate = new DateOnly(2020, 1, 15) },
            new { Ticker = "MSFT", MonthlyAmount = 400m, StartDate = new DateOnly(2020, 6, 10) },
            new { Ticker = "GOOGL", MonthlyAmount = 300m, StartDate = new DateOnly(2021, 1, 5) },
            new { Ticker = "NVDA", MonthlyAmount = 350m, StartDate = new DateOnly(2021, 9, 20) },
            new { Ticker = "TSLA", MonthlyAmount = 250m, StartDate = new DateOnly(2022, 3, 1) }
        };

        // Realistic prices per ticker (base price, will vary over time)
        var basePrices = new Dictionary<string, decimal>
        {
            { "AAPL", 150m },
            { "MSFT", 300m },
            { "GOOGL", 2500m },
            { "NVDA", 450m },
            { "TSLA", 700m }
        };

        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var strategy in dcaStrategies)
        {
            var currentDate = strategy.StartDate;
            var basePrice = basePrices[strategy.Ticker];
            var monthCounter = 0;

            // Generate monthly transactions until today
            while (currentDate <= today)
            {
                // Price varies realistically over time (compound growth with volatility)
                var trendMultiplier = (decimal)Math.Pow(1.02, monthCounter); // 2% compound growth per month
                var volatility = 1m + ((decimal)random.NextDouble() - 0.5m) * 0.3m; // ±15% volatility
                var currentPrice = basePrice * trendMultiplier * volatility;

                // Calculate number of shares
                var transactionCosts = random.Next(1, 5) + (decimal)random.NextDouble(); // €1-€5
                var availableForShares = strategy.MonthlyAmount - transactionCosts;
                var amount = Math.Floor((availableForShares / currentPrice) * 1000m) / 1000m; // 3 decimals

                if (amount > 0)
                {
                    transactions.Add(new TransactionDto
                    {
                        RowKey = Guid.NewGuid().ToString(),
                        Date = currentDate,
                        Ticker = strategy.Ticker,
                        Amount = amount,
                        PurchasePrice = Math.Round(currentPrice, 2),
                        TransactionCosts = Math.Round(transactionCosts, 2)
                    });
                }

                // Move to next month (same day)
                currentDate = currentDate.AddMonths(1);
                monthCounter++;
            }
        }

        return transactions;
    }
}

using Web.Models;

namespace Web.Helpers;

public static class TransactionsFilter
{
    public static List<Transaction> FilterTransactions(List<Transaction> transactions, string? tickers, DateOnly? startDate, DateOnly? endDate)
    {
        if (!string.IsNullOrWhiteSpace(tickers))
        {
            var set = tickers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToUpperInvariant())
                .ToHashSet();

            transactions = transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.Ticker) && set.Contains(t.Ticker.ToUpperInvariant()))
                .ToList();
        }

        if (startDate.HasValue) transactions = transactions.Where(t => t.Date >= startDate.Value).ToList();
        if (endDate.HasValue) transactions = transactions.Where(t => t.Date <= endDate.Value).ToList();

        return transactions;
    }
}

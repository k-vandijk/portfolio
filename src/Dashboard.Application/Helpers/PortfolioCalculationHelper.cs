using Dashboard.Application.Dtos;
using Dashboard.Domain.Utils;

namespace Dashboard.Application.Helpers;

public static class PortfolioCalculationHelper
{
    public static LineChartDto GetPortfolioLineChart(
        List<TransactionDto> transactions,
        List<MarketHistoryDataPointDto> history,
        string title,
        string format,
        Func<decimal, decimal, decimal> selector)
    {
        var transactionsByTicker = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Ticker))
            .GroupBy(t => t.Ticker.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Date).ToList());

        var historyByTicker = history
            .Where(h => !string.IsNullOrWhiteSpace(h.Ticker))
            .GroupBy(h => h.Ticker!.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Date).ToList());

        var allDates = historyByTicker.Values
            .SelectMany(g => g.Select(x => x.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var tickers = transactionsByTicker.Keys.Union(historyByTicker.Keys).ToHashSet();
        var positions = tickers.ToDictionary(t => t, _ => 0m);
        var txIndex = tickers.ToDictionary(t => t, _ => 0);
        var lastPrices = tickers.ToDictionary(t => t, _ => (decimal?)null);

        var priceMap = historyByTicker.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDictionary(x => x.Date, x => x.Close)
        );

        decimal netInvested = 0m;
        var points = new List<DataPointDto>(allDates.Count);

        foreach (var date in allDates)
        {
            foreach (var t in tickers)
            {
                if (!transactionsByTicker.TryGetValue(t, out var txs)) continue;

                while (txIndex[t] < txs.Count && txs[txIndex[t]].Date <= date)
                {
                    var tx = txs[txIndex[t]++];
                    positions[t] += tx.Amount;
                    netInvested += tx.TotalCosts;
                }
            }

            foreach (var t in tickers)
            {
                if (priceMap.TryGetValue(t, out var pricePerDate) &&
                    pricePerDate.TryGetValue(date, out var price))
                {
                    lastPrices[t] = price;
                }
            }

            decimal totalWorth = tickers.Sum(t =>
                lastPrices[t] is decimal p && positions[t] != 0m ? positions[t] * p : 0m);

            decimal y = selector(totalWorth, netInvested);

            points.Add(new DataPointDto
            {
                Label = date.ToString("yyyy-MM-dd"),
                Value = y
            });
        }

        return new LineChartDto
        {
            Title = title,
            DataPoints = points,
            Format = format,
        };
    }

    public static List<DashboardTableRowDto> GetDashboardTableRows(
        List<string> tickers,
        List<TransactionDto> transactions,
        List<MarketHistoryDataPointDto> marketHistoryDataPoints)
    {
        var latestClose = marketHistoryDataPoints
            .GroupBy(p => p.Ticker!.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Date).First().Close
            );

        var aggregates = tickers.Select(ticker =>
        {
            var transactionsByTicker = transactions
                .Where(tr => string.Equals(tr.Ticker, ticker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var amount = transactionsByTicker.Sum(tr => tr.Amount);
            var investment = transactionsByTicker.Sum(tr => tr.TotalCosts);
            var currentPrice = latestClose.GetValueOrDefault(ticker.ToUpperInvariant(), 0m);
            var worth = currentPrice * amount;
            var profit = worth - investment;
            var profitPercentage = investment > 0 ? profit / investment : 0m;

            return new
            {
                Ticker = ticker,
                Amount = amount,
                Investment = investment,
                Worth = worth,
                Profit = profit,
                ProfitPercentage = profitPercentage
            };
        }).ToList();

        var totalWorth = aggregates.Sum(a => a.Worth);

        var rows = aggregates.Select(a =>
        {
            var portfolioPercentage = totalWorth > 0 ? a.Worth / totalWorth : 0m;

            return new DashboardTableRowDto
            {
                Ticker = a.Ticker,
                PortfolioPercentage = portfolioPercentage,
                Amount = a.Amount,
                TotalInvestment = a.Investment,
                Worth = a.Worth,
                Profit = a.Profit,
                ProfitPercentage = a.ProfitPercentage,
            };
        }).ToList();

        return rows;
    }

    public static IReadOnlyList<DataPointDto> NormalizeSeries(IReadOnlyList<DataPointDto> points)
    {
        var first = points.FirstOrDefault()?.Value ?? 0m;
        return points.Select(p => new DataPointDto
        {
            Label = p.Label,
            Value = p.Value - first
        }).ToList();
    }

    public static decimal? GetPeriodDelta(IReadOnlyList<DataPointDto> points, string mode)
    {
        if (points.Count == 0) return null;

        var first = points[0].Value;
        var last = points[^1].Value;

        return mode switch
        {
            DashboardPresentationModes.Value => last - first,
            DashboardPresentationModes.Profit => last,
            DashboardPresentationModes.ProfitPercentage => last,
            _ => null
        };
    }
}

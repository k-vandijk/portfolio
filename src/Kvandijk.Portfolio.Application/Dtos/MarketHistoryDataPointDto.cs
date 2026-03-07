namespace Kvandijk.Portfolio.Application.Dtos;

public class MarketHistoryDataPointDto
{
    public string? Ticker { get; set; } // To join different tickers if needed

    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
}
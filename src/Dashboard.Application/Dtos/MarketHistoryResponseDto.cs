namespace Dashboard.Application.Dtos;

public class MarketHistoryResponseDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<MarketHistoryDataPoint> History { get; set; } = new();
}

public class MarketHistoryDataPoint
{
    public string? Ticker { get; set; } // To join different tickers if needed

    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal Close { get; set; }
}
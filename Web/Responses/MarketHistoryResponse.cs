namespace Web.Responses;

public class MarketHistoryResponse
{
    public string Ticker { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<MarketHistoryDataPoint> History { get; set; } = new();
}

public class MarketHistoryDataPoint
{
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public int Volume { get; set; }
    public decimal Dividends { get; set; }
    public decimal StockSplits { get; set; }
    public decimal CapitalGains { get; set; }
}
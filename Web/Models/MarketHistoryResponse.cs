using System.Text.Json.Serialization;

namespace Web.Models;

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
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] // Volume can be huge; also allow reading from JSON strings if the API sends "123456789"
    public long? Volume { get; set; }
    public decimal Dividends { get; set; }
    public decimal StockSplits { get; set; }
    public decimal CapitalGains { get; set; }
}
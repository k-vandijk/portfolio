namespace Kvandijk.Portfolio.Application.Dtos;

public class MarketHistoryResponseDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<MarketHistoryDataPointDto> History { get; set; } = new();
}

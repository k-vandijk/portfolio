namespace Dashboard.Web.ViewModels;

public class MarketHistoryViewModel
{
    public LineChartViewModel LineChart { get; set; } = new();
    public string CurrentPriceString { get; set; } = string.Empty;
    public string InterestString { get; set; } = string.Empty;
    public string InterestPercentageString { get; set; } = string.Empty;
}

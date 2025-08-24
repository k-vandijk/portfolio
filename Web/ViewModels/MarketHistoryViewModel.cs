namespace Web.ViewModels;

public class MarketHistoryViewModel
{
    public LineChartViewModel LineChart { get; set; }
    public string CurrentPriceString { get; set; }
    public string InterestString { get; set; }
    public string InterestPercentageString { get; set; }
}

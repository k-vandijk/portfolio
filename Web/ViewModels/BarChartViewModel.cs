namespace Web.ViewModels;

public class BarChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<BarChartDataPoint> DataPoints { get; set; } = new ();
}

public class BarChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; } = 0;
}
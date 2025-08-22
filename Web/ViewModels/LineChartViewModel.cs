namespace Web.ViewModels;

public class LineChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<LineChartDataPoint> DataPoints { get; set; } = new ();
}

public class LineChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; } = 0;
}
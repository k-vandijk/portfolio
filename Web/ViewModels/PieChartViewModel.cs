namespace Web.ViewModels;

public class PieChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<PieChartData> Data { get; set; } = new();
}

public class PieChartData
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; } = 0;
}
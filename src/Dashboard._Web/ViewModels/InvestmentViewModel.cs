namespace Dashboard._Web.ViewModels;

public class InvestmentViewModel
{
    public PieChartViewModel PieChart { get; set; } = new();
    public BarChartViewModel BarChart { get; set; } = new();
    public LineChartViewModel LineChart { get; set; } = new();
    public string[] Tickers { get; set; } = [];
    public int[] Years { get; set; } = [];
}
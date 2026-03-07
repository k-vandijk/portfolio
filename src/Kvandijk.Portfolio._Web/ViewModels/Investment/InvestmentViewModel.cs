using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio._Web.ViewModels.Investment;

public class InvestmentViewModel
{
    public PieChartViewModel PieChart { get; set; } = new();
    public BarChartViewModel BarChart { get; set; } = new();
    public LineChartDto LineChart { get; set; } = new();
    public string[] Tickers { get; set; } = [];
    public int[] Years { get; set; } = [];
}

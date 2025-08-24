namespace Web.ViewModels;

public class DashboardViewModel
{
    public List<DashboardTableRowViewModel> TableRows { get; set; }
    public LineChartViewModel LineChart { get; set; }
}

public class DashboardTableRowViewModel
{
    public string Ticker { get; set; }
    public decimal PortfolioPercentage { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalInvestment { get; set; }
    public decimal Worth { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitPercentage { get; set; }
}
namespace Kvandijk.Portfolio.Application.Dtos;

public class DashboardTableRowDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal PortfolioPercentage { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalInvestment { get; set; }
    public decimal Worth { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitPercentage { get; set; }
}

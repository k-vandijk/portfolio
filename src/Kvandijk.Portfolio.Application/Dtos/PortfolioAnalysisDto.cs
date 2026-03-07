namespace Kvandijk.Portfolio.Application.Dtos;

public class PortfolioAnalysisDto
{
    public string RowKey { get; set; } = string.Empty;
    public DateOnly AnalysisDate { get; set; }
    public string AnalysisType { get; set; } = string.Empty;
    public int WeekNumber { get; set; }
    public string Content { get; set; } = string.Empty;
}

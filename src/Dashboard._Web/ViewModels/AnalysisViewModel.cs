using Dashboard.Application.Dtos;

namespace Dashboard._Web.ViewModels;

public class AnalysisViewModel
{
    public List<PortfolioAnalysisDto> WeeklyAnalyses { get; set; } = [];
    public PortfolioAnalysisDto? MonthlyReport { get; set; }

    /// <summary>True when at least one weekly analysis exists this month, enabling monthly report generation.</summary>
    public bool CanGenerateMonthlyReport { get; set; }
}

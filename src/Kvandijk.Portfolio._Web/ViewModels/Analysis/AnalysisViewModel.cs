using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio._Web.ViewModels.Analysis;

public class AnalysisViewModel
{
    public List<PortfolioAnalysisDto> AllAnalyses { get; set; } = [];

    /// <summary>
    /// True when at least one weekly analysis exists this month and no monthly report has been generated yet this month.
    /// </summary>
    public bool CanGenerateMonthlyReport { get; set; }

    public UserSettingsDto Settings { get; set; } = new();
}

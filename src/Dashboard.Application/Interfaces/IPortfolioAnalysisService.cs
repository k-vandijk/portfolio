using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IPortfolioAnalysisService
{
    Task<List<PortfolioAnalysisDto>> GetRecentAnalysesAsync(int count = 4);
    Task RunWeeklyAnalysisAsync();
    Task<PortfolioAnalysisDto> GenerateMonthlyReportAsync();
}

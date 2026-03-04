using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IPortfolioAnalysisService
{
    Task<List<PortfolioAnalysisDto>> GetRecentAnalysesAsync(int count = 4);
    Task<List<PortfolioAnalysisDto>> GetAllAnalysesAsync();
    Task RunWeeklyAnalysisAsync();
    Task<PortfolioAnalysisDto> GenerateMonthlyReportAsync();
    Task DeleteAnalysisAsync(string rowKey);
    Task<string> ChatAsync(string userMessage);
}

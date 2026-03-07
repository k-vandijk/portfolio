using Dashboard.Application.Dtos;

namespace Dashboard.Application.ServiceInterfaces;

public interface IPortfolioAnalysisService
{
    Task RunWeeklyAnalysisAsync();
    Task<PortfolioAnalysisDto> GenerateMonthlyReportAsync();
    Task<string> ChatAsync(string userMessage);
}

using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio.Application.ServiceInterfaces;

public interface IPortfolioAnalysisService
{
    Task RunWeeklyAnalysisAsync();
    Task<PortfolioAnalysisDto> GenerateMonthlyReportAsync();
    Task<string> ChatAsync(string userMessage);
}

using Kvandijk.Portfolio.Domain.Entities;

namespace Kvandijk.Portfolio.Application.RepositoryInterfaces;

public interface IPortfolioAnalysesRepository : IAzureTableRepository<PortfolioAnalysisEntity>
{
    Task<IReadOnlyList<PortfolioAnalysisEntity>> GetWeeklyForCurrentMonthAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PortfolioAnalysisEntity>> GetRecentAnalysesAsync(int count, CancellationToken ct = default );
}
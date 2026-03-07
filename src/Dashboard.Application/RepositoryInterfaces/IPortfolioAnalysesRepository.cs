using Dashboard.Domain.Entities;

namespace Dashboard.Application.RepositoryInterfaces;

public interface IPortfolioAnalysesRepository : IAzureTableRepository<PortfolioAnalysisEntity>
{
}
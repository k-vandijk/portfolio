using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;

namespace Dashboard.Infrastructure.Repositories;

public class PortfolioAnalysesRepository : AzureTableRepository<PortfolioAnalysisEntity>, IPortfolioAnalysesRepository
{
    public PortfolioAnalysesRepository(TableServiceClient serviceClient) : base(serviceClient, StaticDetails.AiAnalysesTableName)
    {
    }
}
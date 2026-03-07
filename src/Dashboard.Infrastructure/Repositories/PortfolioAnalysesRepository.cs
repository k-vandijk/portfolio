using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Infrastructure.Repositories;

public class PortfolioAnalysesRepository : AzureTableRepository<PortfolioAnalysisEntity>, IPortfolioAnalysesRepository
{
    public PortfolioAnalysesRepository(TableServiceClient serviceClient, IMemoryCache cache) : base(serviceClient, StaticDetails.AiAnalysesTableName, cache)
    {
    }

    public async Task<IReadOnlyList<PortfolioAnalysisEntity>> GetWeeklyForCurrentMonthAsync(CancellationToken ct = default)
    {
        var startOfMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
        var filter = $"PartitionKey eq '{StaticDetails.AiAnalysesPartitionKey}' and AnalysisDate ge '{startOfMonth}' and AnalysisType eq 'weekly'";

        var entities = new List<PortfolioAnalysisEntity>();
        await foreach (var entity in Table.QueryAsync<PortfolioAnalysisEntity>(filter: filter, cancellationToken: ct))
            entities.Add(entity);

        return entities;
    }

    public async Task<IReadOnlyList<PortfolioAnalysisEntity>> GetRecentAnalysesAsync(int count, CancellationToken ct = default)
    {
        var entities = await GetAllAsync(ct);
        return entities.OrderByDescending(e => e.AnalysisDate).Take(count).ToList();
    }
}
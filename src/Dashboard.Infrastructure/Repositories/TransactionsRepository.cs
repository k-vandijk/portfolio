using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Infrastructure.Repositories;

public class TransactionsRepository : AzureTableRepository<TransactionEntity>, ITransactionsRepository
{
    public TransactionsRepository(TableServiceClient serviceClient, IMemoryCache cache) : base(serviceClient, StaticDetails.TransactionsTableName, cache)
    {
    }

    public override async Task<IReadOnlyList<TransactionEntity>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await base.GetAllAsync(ct);
        return entities.OrderBy(t => t.Date).ToList();
    }
}
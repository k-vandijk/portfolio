using Azure.Data.Tables;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;
using Kvandijk.Portfolio.Domain.Entities;
using Kvandijk.Portfolio.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Kvandijk.Portfolio.Infrastructure.Repositories;

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
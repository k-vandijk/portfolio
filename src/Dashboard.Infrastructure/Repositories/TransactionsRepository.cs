using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;

namespace Dashboard.Infrastructure.Repositories;

public class TransactionsRepository : AzureTableRepository<TransactionEntity>, ITransactionsRepository
{
    public TransactionsRepository(TableServiceClient serviceClient) : base(serviceClient, StaticDetails.TransactionsTableName)
    {
    }

    public override async Task<IReadOnlyList<TransactionEntity>> GetAllAsync(CancellationToken ct = default)
    {   
        var transactions = new List<TransactionEntity>();

        await foreach (var entity in Table.QueryAsync<TransactionEntity>(cancellationToken: ct))
        {
            transactions.Add(entity);
        }

        var orderedTransactions = transactions.OrderBy(t => t.Date).ToList();

        return orderedTransactions;
    }
}
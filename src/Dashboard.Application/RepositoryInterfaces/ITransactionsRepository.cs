using Dashboard.Domain.Entities;

namespace Dashboard.Application.RepositoryInterfaces;

public interface ITransactionsRepository : IAzureTableRepository<TransactionEntity>
{
}
using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IAzureTableService
{
    Task<List<Transaction>> GetTransactionsAsync();
    Task AddTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(string rowKey);
}
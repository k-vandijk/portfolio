using Dashboard.Domain.Models;

namespace Dashboard.Application.Interfaces;

public interface IAzureTableService
{
    Task<List<Transaction>> GetTransactionsAsync(string connectionString);
    Task AddTransactionAsync(string connectionString, Transaction transaction);
    Task DeleteTransactionAsync(string connectionString, string rowKey);
}
using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetTransactionsAsync();
    Task AddTransactionAsync(TransactionDto transaction);
    Task DeleteTransactionAsync(string rowKey);
}
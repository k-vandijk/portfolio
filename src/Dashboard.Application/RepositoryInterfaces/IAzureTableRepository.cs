namespace Dashboard.Application.RepositoryInterfaces;

public interface IAzureTableRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task DeleteByRowKeyAsync(string rowKey, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
}
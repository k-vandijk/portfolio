using Azure.Data.Tables;

namespace Dashboard.Infrastructure.Repositories;

public abstract class AzureTableRepository<T> where T : class, ITableEntity, new()
{
    protected TableClient Table { get; }

    protected AzureTableRepository(TableServiceClient serviceClient, string tableName)
    {
        Table = serviceClient.GetTableClient(tableName);
        Table.CreateIfNotExists();
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        var list = new List<T>();

        await foreach (var entity in Table.QueryAsync<T>(cancellationToken: ct))
        {
            list.Add(entity);
        }

        return list;
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await Table.AddEntityAsync(entity, ct);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        // Simple version: one-by-one.
        // If you want to optimize later, you can override this and use batch transactions.
        foreach (var e in entities)
        {
            await AddAsync(e, ct);
        }
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        await Table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, cancellationToken: ct);
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        foreach (var e in entities)
        {
            await DeleteAsync(e, ct);
        }
    }

    public virtual async Task DeleteByRowKeyAsync(string rowKey, CancellationToken ct = default)
    {
        // This assumes that RowKey is unique across the table, which may not always be the case.
        // In a real implementation, you might want to handle this differently.
        await foreach (var entity in Table.QueryAsync<T>(e => e.RowKey == rowKey, cancellationToken: ct))
        {
            await DeleteAsync(entity, ct);
        }
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await Table.UpdateEntityAsync(
            entity,
            entity.ETag,
            cancellationToken: ct);
    }
}
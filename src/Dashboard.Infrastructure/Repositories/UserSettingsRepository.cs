using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Dashboard.Infrastructure.Repositories;

public class UserSettingsRepository : AzureTableRepository<UserSettingsEntity>, IUserSettingsRepository
{
    public UserSettingsRepository(TableServiceClient serviceClient, IMemoryCache cache) : base(serviceClient, StaticDetails.UserSettingsTableName, cache)
    {
    }

    public async Task UpsertAsync(UserSettingsEntity entity, CancellationToken ct = default)
    {
        await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        InvalidateCache();
    }
}
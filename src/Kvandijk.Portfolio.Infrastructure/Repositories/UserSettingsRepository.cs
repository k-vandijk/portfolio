using Azure.Data.Tables;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;
using Kvandijk.Portfolio.Domain.Entities;
using Kvandijk.Portfolio.Domain.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace Kvandijk.Portfolio.Infrastructure.Repositories;

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
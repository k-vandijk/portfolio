using Kvandijk.Portfolio.Domain.Entities;

namespace Kvandijk.Portfolio.Application.RepositoryInterfaces;

public interface IUserSettingsRepository : IAzureTableRepository<UserSettingsEntity>
{
    Task UpsertAsync(UserSettingsEntity entity, CancellationToken ct = default);
}
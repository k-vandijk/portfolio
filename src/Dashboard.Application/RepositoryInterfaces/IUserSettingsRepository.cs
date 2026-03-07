using Dashboard.Domain.Entities;

namespace Dashboard.Application.RepositoryInterfaces;

public interface IUserSettingsRepository : IAzureTableRepository<UserSettingsEntity>
{
    Task UpsertAsync(UserSettingsEntity entity, CancellationToken ct = default);
}
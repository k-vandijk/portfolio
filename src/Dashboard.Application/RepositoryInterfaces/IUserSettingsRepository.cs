using Dashboard.Domain.Entities;

namespace Dashboard.Application.RepositoryInterfaces;

public interface IUserSettingsRepository : IAzureTableRepository<UserSettingsEntity>
{
}
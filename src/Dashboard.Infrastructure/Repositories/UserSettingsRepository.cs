using Azure.Data.Tables;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;

namespace Dashboard.Infrastructure.Repositories;

public class UserSettingsRepository : AzureTableRepository<UserSettingsEntity>, IUserSettingsRepository
{
    public UserSettingsRepository(TableServiceClient serviceClient) : base(serviceClient, StaticDetails.UserSettingsTableName)
    {
    }
}
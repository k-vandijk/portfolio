using Azure;
using Azure.Data.Tables;
using Dashboard.Application.Dtos;
using Dashboard.Application.Mappers;
using Dashboard.Application.ServiceInterfaces;
using Dashboard.Domain.Entities;
using Dashboard.Domain.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly TableClient _table;

    public UserSettingsService([FromKeyedServices(StaticDetails.UserSettingsTableName)] TableClient table)
    {
        _table = table;
    }

    public async Task<UserSettingsDto> GetSettingsAsync()
    {
        try
        {
            var response = await _table.GetEntityAsync<UserSettingsEntity>(
                StaticDetails.UserSettingsPartitionKey,
                StaticDetails.UserSettingsRowKey);

            return response.Value.ToDto();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new UserSettingsDto();
        }
    }

    public async Task SaveSettingsAsync(UserSettingsDto settings)
    {
        var entity = settings.ToEntity();
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }
}

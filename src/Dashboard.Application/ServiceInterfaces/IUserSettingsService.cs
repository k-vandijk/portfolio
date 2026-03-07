using Dashboard.Application.Dtos;

namespace Dashboard.Application.ServiceInterfaces;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(UserSettingsDto settings);
}

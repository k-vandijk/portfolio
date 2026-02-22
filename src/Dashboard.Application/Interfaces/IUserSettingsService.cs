using Dashboard.Application.Dtos;

namespace Dashboard.Application.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(UserSettingsDto settings);
}

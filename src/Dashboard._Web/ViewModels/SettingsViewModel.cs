using Dashboard.Application.Dtos;

namespace Dashboard._Web.ViewModels;

public class SettingsViewModel
{
    public UserSettingsDto Settings { get; set; } = new();
}

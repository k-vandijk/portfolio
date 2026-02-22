using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard._Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class SettingsController : Controller
{
    private readonly IUserSettingsService _settingsService;

    public SettingsController(IUserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("/settings")]
    public IActionResult Index() => View();

    [HttpGet("/settings/content")]
    public async Task<IActionResult> SettingsContent()
    {
        var settings = await _settingsService.GetSettingsAsync();
        var viewModel = new SettingsViewModel { Settings = settings };
        return PartialView("_SettingsContent", viewModel);
    }

    [HttpPost("/settings/save")]
    public async Task<IActionResult> SaveSettings([FromBody] UserSettingsDto settings)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _settingsService.SaveSettingsAsync(settings);
        return Ok();
    }
}

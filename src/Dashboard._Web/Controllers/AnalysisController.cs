using Dashboard.Application.Dtos;
using Dashboard._Web.Helpers;
using Dashboard._Web.ViewModels;
using Dashboard.Application.Mappers;
using Dashboard.Application.RepositoryInterfaces;
using Dashboard.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class AnalysisController : Controller
{
    private readonly IPortfolioAnalysisService _analysisService;
    private readonly IUserSettingsRepository _userSettingsRepository;

    public AnalysisController(IPortfolioAnalysisService analysisService, IUserSettingsRepository userSettingsRepository)
    {
        _analysisService = analysisService;
        _userSettingsRepository = userSettingsRepository;
    }

    [HttpGet("/analysis")]
    [HttpGet("/analyse")]
    public IActionResult Index() => View();

    [HttpGet("/analysis/content")]
    public async Task<IActionResult> AnalysisContent()
    {
        var allAnalyses = await _analysisService.GetAllAnalysesAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var hasWeeklyThisMonth = allAnalyses.Any(a =>
            a.AnalysisType == "weekly" &&
            a.AnalysisDate.Year == today.Year &&
            a.AnalysisDate.Month == today.Month);

        var hasMonthlyThisMonth = allAnalyses.Any(a =>
            a.AnalysisType == "monthly" &&
            a.AnalysisDate.Year == today.Year &&
            a.AnalysisDate.Month == today.Month);

        var settingsEntities = await _userSettingsRepository.GetAllAsync();
        var settings = settingsEntities.FirstOrDefault()?.ToDto() ?? new UserSettingsDto();

        var viewModel = new AnalysisViewModel
        {
            AllAnalyses = allAnalyses,
            CanGenerateMonthlyReport = hasWeeklyThisMonth && !hasMonthlyThisMonth,
            Settings = settings
        };

        return PartialView("_AnalysisContent", viewModel);
    }

    [HttpPost("/analysis/monthly")]
    public async Task<IActionResult> GenerateMonthly()
    {
        await _analysisService.GenerateMonthlyReportAsync();
        return Ok();
    }

    [HttpPost("/analysis/settings")]
    public async Task<IActionResult> SaveSettings([FromBody] UserSettingsDto settings)
    {
        string[] validRisk = ["conservative", "moderate", "aggressive"];
        string[] validHorizon = ["short", "medium", "long"];

        if (!validRisk.Contains(settings.RiskTolerance) || !validHorizon.Contains(settings.InvestmentHorizon))
            return BadRequest("Invalid settings values.");

        await _userSettingsRepository.UpsertAsync(settings.ToEntity());
        return Ok();
    }

    [HttpDelete("/analysis/{rowKey}")]
    public async Task<IActionResult> DeleteAnalysis(string rowKey)
    {
        await _analysisService.DeleteAnalysisAsync(rowKey);
        return Ok();
    }

    [HttpPost("/analysis/chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
            return BadRequest("Message cannot be empty.");

        if (request.Message.Length > 2000)
            return BadRequest("Message cannot exceed 2000 characters.");

        var response = await _analysisService.ChatAsync(request.Message);
        return Ok(new { html = MarkdownHelper.ToHtml(response) });
    }

    [HttpPost("/analysis/run-weekly")]
    public async Task<IActionResult> RunWeeklyAnalysis()
    {
        await _analysisService.RunWeeklyAnalysisAsync();
        return Ok();
    }
}

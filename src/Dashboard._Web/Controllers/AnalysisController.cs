using Dashboard.Application.Dtos;
using Dashboard.Application.Interfaces;
using Dashboard._Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class AnalysisController : Controller
{
    private readonly IPortfolioAnalysisService _analysisService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(IPortfolioAnalysisService analysisService, IUserSettingsService userSettingsService, ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _userSettingsService = userSettingsService;
        _logger = logger;
    }

    [HttpGet("/analysis")]
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

        var settings = await _userSettingsService.GetSettingsAsync();

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
        try
        {
            await _analysisService.GenerateMonthlyReportAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate monthly report");
            return StatusCode(500, "Failed to generate the monthly report. Please try again.");
        }
    }

    [HttpPost("/analysis/settings")]
    public async Task<IActionResult> SaveSettings([FromBody] UserSettingsDto settings)
    {
        string[] validRisk = ["conservative", "moderate", "aggressive"];
        string[] validHorizon = ["short", "medium", "long"];

        if (!validRisk.Contains(settings.RiskTolerance) || !validHorizon.Contains(settings.InvestmentHorizon))
            return BadRequest("Invalid settings values.");

        await _userSettingsService.SaveSettingsAsync(settings);
        return Ok();
    }

    [HttpDelete("/analysis/{rowKey}")]
    public async Task<IActionResult> DeleteAnalysis(string rowKey)
    {
        try
        {
            await _analysisService.DeleteAnalysisAsync(rowKey);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete analysis {RowKey}", rowKey);
            return StatusCode(500, "Failed to delete the analysis. Please try again.");
        }
    }

    [HttpPost("/analysis/run-weekly")]
    public async Task<IActionResult> RunWeeklyAnalysis()
    {
        try
        {
            await _analysisService.RunWeeklyAnalysisAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run weekly analysis");
            return StatusCode(500, "Failed to run the weekly analysis. Please try again.");
        }
    }
}

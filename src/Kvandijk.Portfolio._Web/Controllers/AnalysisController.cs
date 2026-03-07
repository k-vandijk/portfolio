using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio._Web.ViewModels.Analysis;
using Kvandijk.Portfolio.Application.Helpers;
using Kvandijk.Portfolio.Application.Mappers;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;
using Kvandijk.Portfolio.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kvandijk.Portfolio._Web.Controllers;

public class AnalysisController : Controller
{
    private readonly IPortfolioAnalysisService _analysisService;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly IPortfolioAnalysesRepository _analysesRepository;

    public AnalysisController(IPortfolioAnalysisService analysisService, IUserSettingsRepository userSettingsRepository, IPortfolioAnalysesRepository analysesRepository)
    {
        _analysisService = analysisService;
        _userSettingsRepository = userSettingsRepository;
        _analysesRepository = analysesRepository;
    }

    [HttpGet("/analysis")]
    [HttpGet("/analyse")]
    public IActionResult Index() => View();

    [HttpGet("/analysis/content")]
    public async Task<IActionResult> AnalysisContent()
    {
        var analysesEntities = await _analysesRepository.GetAllAsync();
        var analysesDtos = analysesEntities.Select(e => e.ToDto()).ToList();
        var orderedAnalyses = analysesDtos.OrderByDescending(a => a.AnalysisDate).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var hasWeeklyThisMonth = orderedAnalyses.Any(a =>
            a.AnalysisType == "weekly" &&
            a.AnalysisDate.Year == today.Year &&
            a.AnalysisDate.Month == today.Month);

        var hasMonthlyThisMonth = orderedAnalyses.Any(a =>
            a.AnalysisType == "monthly" &&
            a.AnalysisDate.Year == today.Year &&
            a.AnalysisDate.Month == today.Month);

        var settingsEntities = await _userSettingsRepository.GetAllAsync();
        var settings = settingsEntities.FirstOrDefault()?.ToDto() ?? new UserSettingsDto();

        var viewModel = new AnalysisViewModel
        {
            AllAnalyses = orderedAnalyses,
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
        await _analysesRepository.DeleteByRowKeyAsync(rowKey);
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

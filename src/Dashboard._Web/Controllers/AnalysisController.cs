using Dashboard.Application.Interfaces;
using Dashboard._Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard._Web.Controllers;

public class AnalysisController : Controller
{
    private readonly IPortfolioAnalysisService _analysisService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(IPortfolioAnalysisService analysisService, ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpGet("/analysis")]
    public IActionResult Index() => View();

    [HttpGet("/analysis/content")]
    public async Task<IActionResult> AnalysisContent()
    {
        var analyses = await _analysisService.GetRecentAnalysesAsync(8);

        var weeklyAnalyses = analyses
            .Where(a => a.AnalysisType == "weekly")
            .OrderByDescending(a => a.AnalysisDate)
            .ToList();

        var monthlyReport = analyses
            .Where(a => a.AnalysisType == "monthly")
            .OrderByDescending(a => a.AnalysisDate)
            .FirstOrDefault();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var canGenerate = weeklyAnalyses.Any(a =>
            a.AnalysisDate.Year == today.Year &&
            a.AnalysisDate.Month == today.Month);

        var viewModel = new AnalysisViewModel
        {
            WeeklyAnalyses = weeklyAnalyses,
            MonthlyReport = monthlyReport,
            CanGenerateMonthlyReport = canGenerate
        };

        return PartialView("_AnalysisContent", viewModel);
    }

    [HttpPost("/analysis/monthly")]
    public async Task<IActionResult> GenerateMonthly()
    {
        try
        {
            var report = await _analysisService.GenerateMonthlyReportAsync();

            var viewModel = new AnalysisViewModel
            {
                WeeklyAnalyses = [],
                MonthlyReport = report,
                CanGenerateMonthlyReport = false
            };

            return PartialView("_MonthlyReport", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate monthly report");
            return StatusCode(500, "Failed to generate the monthly report. Please try again.");
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

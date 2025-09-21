using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
using Dashboard.Application.Interfaces;
using Dashboard.Domain.Models;
using Dashboard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Dashboard.Web.Controllers;

public class InvestmentController : Controller
{
    private readonly IAzureTableService _service;
    private readonly ILogger<InvestmentController> _logger;
    private readonly IConfiguration _config;

    public InvestmentController(IAzureTableService service, ILogger<InvestmentController> logger, IConfiguration config)
    {
        _service = service;
        _logger = logger;
        _config = config;
    }

    [HttpGet("/investment")]
    public IActionResult Investment(
        [FromQuery] string? tickers,
        [FromQuery] string? timerange)
    {
        return View();
    }

    [HttpGet("/investment/section")]
    public async Task<IActionResult> InvestmentSection(
        [FromQuery] string? tickers, 
        [FromQuery] string? timerange)
    {
        var sw = Stopwatch.StartNew();

        var connectionString = _config["Secrets:TransactionsTableConnectionString"]
            ?? throw new ArgumentNullException("Secrets:TransactionsTableConnectionString", "Please set the connection string in the configuration.");

        var transactions = await _service.GetTransactionsAsync(connectionString);
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromTimeRange(timerange ?? "ALL");
        var filteredTransactions = FilterHelper.FilterTransactions(transactions, tickers, startDate, endDate);

        var pieChartViewModel = GetPieChartViewModel(filteredTransactions);
        var barChartViewModel = GetBarChartViewModel(filteredTransactions);
        var lineChartViewModel = GetLineChartViewModel(filteredTransactions);

        var viewModel = new InvestmentViewModel
        {
            PieChart = pieChartViewModel,
            BarChart = barChartViewModel,
            LineChart = lineChartViewModel
        };

        sw.Stop();
        _logger.LogInformation("Investment view rendered in {Elapsed} ms", sw.ElapsedMilliseconds);
        return PartialView("_InvestmentSection", viewModel);
    }

    private LineChartViewModel GetLineChartViewModel(List<Transaction> transactions)
    {
        var cumulativeSum = 0m;
        var groupedTransactions = transactions
            .GroupBy(t => t.Date)
            .Select(g =>
            {
                cumulativeSum += g.Sum(t => t.TotalCosts);
                return new LineChartDataPointDto
                {
                    Label = g.Key.ToString("yyyy-MM-dd"),
                    Value = cumulativeSum
                };
            })
            .OrderBy(dp => dp.Label)
            .ToList();

        var lineChartViewModel = new LineChartViewModel
        {
            Title = "Investment (cumulative) per month",
            DataPoints = groupedTransactions,
            Format = "currency"
        };

        return lineChartViewModel;
    }

    private PieChartViewModel GetPieChartViewModel(List<Transaction> transactions)
    {
        var groupedTransactions = transactions
            .GroupBy(t => t.Ticker)
            .Select(g => new PieChartDataPointDto
            {
                Label = g.Key,
                Value = g.Sum(t => t.TotalCosts)
            })
            .ToList();

        var pieChartViewModel = new PieChartViewModel
        {
            Title = "Investment per ticker",
            Data = groupedTransactions
        };

        return pieChartViewModel;
    }

    private BarChartViewModel GetBarChartViewModel(List<Transaction> transactions)
    {
        var groupedTransactions = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new BarChartDataPointDto
            {
                Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                Value = g.Sum(t => t.TotalCosts)
            })
            .OrderBy(dp => dp.Label)
            .ToList();

        var barChartViewModel = new BarChartViewModel
        {
            Title = "Investment per month",
            DataPoints = groupedTransactions
        };

        return barChartViewModel;
    }
}
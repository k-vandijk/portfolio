using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
using Dashboard.Application.Interfaces;
using Dashboard.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Dashboard.Web.Controllers;

public class InvestmentController : Controller
{
    private readonly IAzureTableService _service;
    private readonly ILogger<InvestmentController> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public InvestmentController(IAzureTableService service, ILogger<InvestmentController> logger, IStringLocalizer<SharedResource> localizer)
    {
        _service = service;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet("/investment")]
    public IActionResult Index() => View();

    [HttpGet("/investment/content")]
    public async Task<IActionResult> InvestmentContent(
        [FromQuery] string? tickers,    
        [FromQuery] int? year)
    {
        var transactions = await _service.GetTransactionsAsync();
        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(year ?? DateTime.UtcNow.Year);
        var filteredTransactions = FilterHelper.FilterTransactions(transactions, tickers, startDate, endDate);

        var pieChartViewModel = GetPieChartViewModel(filteredTransactions);
        var barChartViewModel = GetBarChartViewModel(filteredTransactions);
        var lineChartViewModel = GetLineChartViewModel(filteredTransactions);

        var viewModel = new InvestmentViewModel
        {
            PieChart = pieChartViewModel,
            BarChart = barChartViewModel,
            LineChart = lineChartViewModel,
            Tickers = transactions.Select(t => t.Ticker).Distinct().OrderBy(t => t).ToArray(),
            Years = transactions.Select(t => t.Date.Year).Distinct().OrderBy(y => y).ToArray()
        };

        return PartialView("_InvestmentContent", viewModel);
    }

    private LineChartViewModel GetLineChartViewModel(List<Transaction> transactions)
    {
        var cumulativeSum = 0m;
        var groupedTransactions = transactions
            .GroupBy(t => t.Date)
            .Select(g =>
            {
                cumulativeSum += g.Sum(t => t.TotalCosts);
                return new DataPointDto
                {
                    Label = g.Key.ToString("yyyy-MM-dd"),
                    Value = cumulativeSum
                };
            })
            .OrderBy(dp => dp.Label)
            .ToList();

        var lineChartViewModel = new LineChartViewModel
        {
            Title = _localizer["InvestmentPerMonth"],
            DataPoints = groupedTransactions,
            Format = "currency",
        };

        return lineChartViewModel;
    }

    private PieChartViewModel GetPieChartViewModel(List<Transaction> transactions)
    {
        var groupedTransactions = transactions
            .GroupBy(t => t.Ticker)
            .Select(g => new DataPointDto
            {
                Label = g.Key,
                Value = g.Sum(t => t.TotalCosts)
            })
            .ToList();

        var pieChartViewModel = new PieChartViewModel
        {
            Title = _localizer["InvestmentPerTicker"],
            Data = groupedTransactions
        };

        return pieChartViewModel;
    }

    private BarChartViewModel GetBarChartViewModel(List<Transaction> transactions)
    {
        var groupedTransactions = transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new DataPointDto
            {
                Label = $"{g.Key.Year}-{g.Key.Month:D2}",
                Value = g.Sum(t => t.TotalCosts)
            })
            .OrderBy(dp => dp.Label)
            .ToList();

        var barChartViewModel = new BarChartViewModel
        {
            Title = _localizer["InvestmentPerMonth"],
            DataPoints = groupedTransactions,
            ShowAverageLine = true
        };

        return barChartViewModel;
    }
}
using Microsoft.AspNetCore.Mvc;
using Web.Helpers;
using Web.Models;
using Web.Services;
using Web.ViewModels;

namespace Web.Controllers;

public class InvestmentController : Controller
{
    private readonly IAzureTableService _service;

    public InvestmentController(IAzureTableService service)
    {
        _service = service;
    }

    [HttpGet("/investment")]
    public IActionResult Investment(
        [FromQuery] string? tickers, 
        [FromQuery] DateOnly? startDate, 
        [FromQuery] DateOnly? endDate)
    {
        var transactions = _service.GetTransactions();
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

        return View(viewModel);
    }

    private LineChartViewModel GetLineChartViewModel(List<Transaction> transactions)
    {
        var cumulativeSum = 0m;
        var groupedTransactions = transactions
            .GroupBy(t => t.Date)
            .Select(g =>
            {
                cumulativeSum += g.Sum(t => t.TotalCosts);
                return new LineChartDataPoint
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
            .Select(g => new PieChartDataPoint
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
            .Select(g => new BarChartDataPoint
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
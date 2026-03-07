using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio.Application.Helpers;
using Kvandijk.Portfolio._Web.ViewModels;
using Kvandijk.Portfolio._Web.ViewModels.Investment;
using Kvandijk.Portfolio.Application.Mappers;
using Kvandijk.Portfolio.Application.RepositoryInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Kvandijk.Portfolio._Web.Controllers;

public class InvestmentController : Controller
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ITransactionsRepository _transactionsRepository;

    public InvestmentController(IStringLocalizer<SharedResource> localizer, ITransactionsRepository transactionsRepository)
    {
        _localizer = localizer;
        _transactionsRepository = transactionsRepository;
    }

    [HttpGet("/investment")]
    [HttpGet("/investering")]
    public IActionResult Index() => View();

    [HttpGet("/investment/content")]
    public async Task<IActionResult> InvestmentContent(
        [FromQuery] string? tickers,    
        [FromQuery] int? year)
    {
        //var transactions = await _service.GetTransactionsAsync();
        var transactionEntities = await _transactionsRepository.GetAllAsync();
        var transactionDtos = transactionEntities.Select(e => e.ToModel()).ToList();

        var (startDate, endDate) = FilterHelper.GetMinMaxDatesFromYear(year ?? DateTime.UtcNow.Year);
        var filteredTransactions = FilterHelper.FilterTransactions(transactionDtos, tickers, startDate, endDate);

        var pieChartViewModel = GetPieChartViewModel(filteredTransactions);
        var barChartViewModel = GetBarChartViewModel(filteredTransactions);
        var lineChartViewModel = GetLineChartDto(filteredTransactions);

        var viewModel = new InvestmentViewModel
        {
            PieChart = pieChartViewModel,
            BarChart = barChartViewModel,
            LineChart = lineChartViewModel,
            Tickers = transactionDtos.Select(t => t.Ticker).Distinct().OrderBy(t => t).ToArray(),
            Years = transactionDtos.Select(t => t.Date.Year).Distinct().OrderBy(y => y).ToArray()
        };

        return PartialView("_InvestmentContent", viewModel);
    }

    private LineChartDto GetLineChartDto(List<TransactionDto> transactions)
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

        var lineChartViewModel = new LineChartDto
        {
            Title = _localizer["InvestmentPerMonth"],
            DataPoints = groupedTransactions,
            Format = "currency",
        };

        return lineChartViewModel;
    }

    private PieChartViewModel GetPieChartViewModel(List<TransactionDto> transactions)
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

    private BarChartViewModel GetBarChartViewModel(List<TransactionDto> transactions)
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
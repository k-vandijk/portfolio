using Microsoft.Extensions.Localization;

namespace Dashboard._Web.ViewModels.Investment;

public class InvestmentFiltersContentViewModel
{
    public required IStringLocalizer SharedLocalizer { get; init; }

    public required IEnumerable<string> Tickers { get; init; }
    public required IEnumerable<int> Years { get; init; }

    public string? CurrentTickersString { get; init; }
    public required int CurrentYear { get; init; }

    public required IEnumerable<string> SelectedTickers { get; init; }

    // Used to filter out tickers not invested in that year
    public required ISet<string> PieChartLabels { get; init; }

    public required string SystemUrlInvestment { get; init; }
}
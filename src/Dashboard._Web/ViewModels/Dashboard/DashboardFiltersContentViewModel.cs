using Microsoft.Extensions.Localization;

namespace Dashboard._Web.ViewModels.Dashboard;

public class DashboardFiltersContentViewModel
{
    public required IStringLocalizer SharedLocalizer { get; init; }

    public required IReadOnlyDictionary<string, string> Modes { get; init; }
    public required IEnumerable<string> Timeranges { get; init; }
    public required IEnumerable<int> Years { get; init; }

    public required string CurrentTimerange { get; init; }
    public required string CurrentMode { get; init; }
    public string? CurrentTickersString { get; init; }
    public int? CurrentYear { get; init; }
}
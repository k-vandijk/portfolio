namespace Kvandijk.Portfolio._Web.ViewModels;

public class MobileFilterFabDropdownViewModel
{
    public required string ButtonId { get; init; }
    public string AriaLabel { get; init; } = "Filters";
    public required string ContentPartial { get; init; }
    public required object ContentModel { get; init; }
}
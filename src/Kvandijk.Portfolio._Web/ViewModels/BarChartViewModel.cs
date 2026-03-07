using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio._Web.ViewModels;

public class BarChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<DataPointDto> DataPoints { get; set; } = new ();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
    public bool ShowAverageLine { get; set; } = false;
}


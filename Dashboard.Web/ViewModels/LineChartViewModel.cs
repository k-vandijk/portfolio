using Dashboard.Application.Dtos;

namespace Dashboard.Web.ViewModels;

public class LineChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<LineChartDataPointDto> DataPoints { get; set; } = new ();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
    public decimal? Profit { get; set; }
}

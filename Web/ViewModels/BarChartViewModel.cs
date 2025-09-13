using Dashboard.Application.Dtos;

namespace Dashboard.Web.ViewModels;

public class BarChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<BarChartDataPointDto> DataPoints { get; set; } = new ();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
}


using Dashboard.Application.Dtos;

namespace Dashboard.Web.ViewModels;

public class PieChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<PieChartDataPointDto> Data { get; set; } = new();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
}


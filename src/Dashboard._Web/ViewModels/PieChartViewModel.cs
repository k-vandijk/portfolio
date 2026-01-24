using Dashboard.Application.Dtos;

namespace Dashboard._Web.ViewModels;

public class PieChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<DataPointDto> Data { get; set; } = new();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
}


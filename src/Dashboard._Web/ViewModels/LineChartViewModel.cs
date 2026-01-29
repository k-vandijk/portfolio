using Dashboard.Application.Dtos;

namespace Dashboard._Web.ViewModels;

public class LineChartViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<DataPointDto> DataPoints { get; set; } = new ();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
    public decimal? Profit { get; set; }
    public string Mode { get; set; } = "profit"; // "value" | "profit" | "profit-percentage" - default matches controller
}

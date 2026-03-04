namespace Dashboard.Application.Dtos;

public class LineChartDto
{
    public string Title { get; set; } = string.Empty;
    public List<DataPointDto> DataPoints { get; set; } = new ();
    public string Format { get; set; } = "currency"; // "currency" | "percentage" | "number"
    public decimal? Profit { get; set; }
}

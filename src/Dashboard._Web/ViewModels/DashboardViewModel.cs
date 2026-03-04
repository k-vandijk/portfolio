using Dashboard.Application.Dtos;

namespace Dashboard._Web.ViewModels;

public class DashboardViewModel
{
    public List<DashboardTableRowDto> TableRows { get; set; } = new();
    public LineChartDto LineChart { get; set; } = new();
    public int[] Years { get; set; } = [];
}

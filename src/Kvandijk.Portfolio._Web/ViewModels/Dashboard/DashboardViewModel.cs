using Kvandijk.Portfolio.Application.Dtos;

namespace Kvandijk.Portfolio._Web.ViewModels.Dashboard;

public class DashboardViewModel
{
    public List<DashboardTableRowDto> TableRows { get; set; } = new();
    public LineChartDto LineChart { get; set; } = new();
    public int[] Years { get; set; } = [];
}

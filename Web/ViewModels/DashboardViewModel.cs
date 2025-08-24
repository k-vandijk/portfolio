using Web.Models;

namespace Web.ViewModels;

public class DashboardViewModel
{
    public TableViewModel Table { get; set; }
    public LineChartViewModel LineChart { get; set; }
}

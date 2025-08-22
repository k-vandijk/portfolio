namespace Web.ViewModels;

public class TableViewModel
{
    public string Title { get; set; } = "";
    public List<TableColumn> Columns { get; set; } = new();
    public IEnumerable<IDictionary<string, object?>> Rows { get; set; } = Array.Empty<IDictionary<string, object?>>();
    public string EmptyText { get; set; } = "No data";
}

public class TableColumn
{
    public string Header { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

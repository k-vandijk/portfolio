using Azure;
using Azure.Data.Tables;

namespace Kvandijk.Portfolio.Domain.Entities;

public class PortfolioAnalysisEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>ISO 8601 date when this analysis was generated (yyyy-MM-dd).</summary>
    public string AnalysisDate { get; set; } = string.Empty;

    /// <summary>"weekly" or "monthly"</summary>
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>Week number within the month (1–4). 0 for monthly reports.</summary>
    public string WeekNumber { get; set; } = string.Empty;

    /// <summary>AI-generated narrative text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>JSON snapshot of holdings at the time of analysis.</summary>
    public string PortfolioSnapshot { get; set; } = string.Empty;
}

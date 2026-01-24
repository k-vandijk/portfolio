using System.ComponentModel.DataAnnotations;

namespace Dashboard.Application.Dtos;

public class TransactionDto
{
    public string? RowKey { get; set; }

    [Required] public DateOnly Date { get; set; }
    [Required] public string Ticker { get; set; } = string.Empty;
    [Required] public decimal Amount { get; set; }
    [Required] public decimal PurchasePrice { get; set; }
    [Required] public decimal TransactionCosts { get; set; }

    public decimal TotalCosts => Amount * PurchasePrice + TransactionCosts;
}

using System.ComponentModel.DataAnnotations;

namespace Dashboard.Domain.Models;

public class Transaction
{
    public string? RowKey { get; set; }

    [Required] public DateOnly Date { get; set; }
    [Required] public string Ticker { get; set; } = string.Empty;
    [Required] public decimal Amount { get; set; }
    [Required] public decimal PurchasePrice { get; set; }
    [Required] public decimal TransactionCosts { get; set; }

    public decimal TotalCosts => Amount * PurchasePrice + TransactionCosts;
}

namespace Web.Models;

public class Transaction
{
    public string? RowKey { get; set; }

    public DateOnly Date { get; set; }
    public string Ticker { get; set; }
    public decimal Amount { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal TransactionCosts { get; set; }

    public decimal TotalCosts => (Amount * PurchasePrice) + TransactionCosts;
}

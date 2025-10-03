using Azure;
using Dashboard.Application.Dtos;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;

namespace Dashboard.Application.Helpers;

public static class TransactionMapper
{
    public static TransactionEntity ToEntity(this Transaction t)
    {
        return new TransactionEntity
        {
            PartitionKey = StaticDetails.PartitionKey,
            RowKey = string.IsNullOrWhiteSpace(t.RowKey) ? Guid.NewGuid().ToString("N") : t.RowKey,
            Date = FormattingHelper.FormatDate(t.Date),
            Ticker = t.Ticker,
            Amount = FormattingHelper.FormatDecimal(t.Amount),
            PurchasePrice = FormattingHelper.FormatDecimal(t.PurchasePrice),
            TransactionCosts = FormattingHelper.FormatDecimal(t.TransactionCosts),
            ETag = ETag.All
        };
    }

    public static Transaction ToModel(this TransactionEntity e)
    {
        return new Transaction
        {
            RowKey = e.RowKey,
            Date = FormattingHelper.ParseDateOnly(e.Date),
            Ticker = e.Ticker,
            Amount = FormattingHelper.ParseDecimal(e.Amount),
            PurchasePrice = FormattingHelper.ParseDecimal(e.PurchasePrice),
            TransactionCosts = FormattingHelper.ParseDecimal(e.TransactionCosts)
        };
    }
}
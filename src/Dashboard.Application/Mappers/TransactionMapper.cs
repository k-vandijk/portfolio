using Azure;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;

namespace Dashboard.Application.Mappers;

public static class TransactionMapper
{
    public static TransactionEntity ToEntity(this TransactionDto t)
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

    public static TransactionDto ToModel(this TransactionEntity e)
    {
        return new TransactionDto
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
namespace Kvandijk.Portfolio.Application.Dtos;

public record HoldingInfo(string Ticker, decimal Quantity, decimal CurrentPrice, decimal TotalValue, decimal PreviousDayClose);
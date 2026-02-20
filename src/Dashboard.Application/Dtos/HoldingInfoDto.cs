namespace Dashboard.Application.Dtos;

public record HoldingInfo(string Ticker, decimal Quantity, decimal CurrentPrice, decimal TotalValue, decimal PreviousDayClose);
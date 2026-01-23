using Azure;
using Dashboard.Application.Dtos;
using Dashboard.Application.Helpers;
using Dashboard.Domain.Models;
using Dashboard.Domain.Utils;

namespace Dashboard.Tests.Application.Helpers;

public class TransactionMapperTests
{
    [Fact]
    public void Maps_All_Fields_Including_Invariant_Formats()
    {
        var model = new Transaction
        {
            RowKey = "rk-123",
            Date = new DateOnly(2024, 05, 17),
            Ticker = "MSFT",
            Amount = 12.3400m,
            PurchasePrice = 1000m,
            TransactionCosts = -0.50m
        };

        var entity = model.ToEntity();

        Assert.Equal(StaticDetails.PartitionKey, entity.PartitionKey);
        Assert.Equal("rk-123", entity.RowKey);
        Assert.Equal("2024-05-17", entity.Date);            // FormatDate => yyyy-MM-dd
        Assert.Equal("MSFT", entity.Ticker);
        Assert.Equal("12.34", entity.Amount);               // trimmed trailing zeros
        Assert.Equal("1000", entity.PurchasePrice);         // no thousands separator
        Assert.Equal("-0.5", entity.TransactionCosts);      // invariant decimal point
        Assert.Equal(ETag.All, entity.ETag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generates_New_RowKey_When_Missing(string? rowKey)
    {
        var model = new Transaction
        {
            RowKey = rowKey,
            Date = new DateOnly(2024, 1, 1),
            Ticker = "ABC",
            Amount = 1m,
            PurchasePrice = 2m,
            TransactionCosts = 3m
        };

        var entity = model.ToEntity();

        Assert.False(string.IsNullOrWhiteSpace(entity.RowKey));
        // Should be a Guid in "N" format (32 hex chars, no hyphens)
        Assert.True(Guid.TryParseExact(entity.RowKey, "N", out _));
    }

    [Fact]
    public void Default_Date_Becomes_Empty_String()
    {
        var model = new Transaction
        {
            RowKey = "rk",
            Date = default,
            Ticker = "X",
            Amount = 0m,
            PurchasePrice = 0m,
            TransactionCosts = 0m
        };

        var entity = model.ToEntity();

        Assert.Equal(string.Empty, entity.Date);
    }

    [Fact]
    public void Maps_All_Fields_With_Tolerant_Parsing()
    {
        var entity = new TransactionEntity
        {
            PartitionKey = StaticDetails.PartitionKey,
            RowKey = "rk-xyz",
            Date = "2024-05-17",
            Ticker = "MSFT",
            Amount = "12.34",          // invariant path
            PurchasePrice = "1.234,56",// nl-NL fallback path => 1234.56
            TransactionCosts = "-0,5", // nl-NL fallback path => -0.5
            ETag = ETag.All
        };

        var model = entity.ToModel();

        Assert.Equal("rk-xyz", model.RowKey);
        Assert.Equal(new DateOnly(2024, 05, 17), model.Date);
        Assert.Equal("MSFT", model.Ticker);
        Assert.Equal(12.34m, model.Amount);
        Assert.Equal(1234.56m, model.PurchasePrice);
        Assert.Equal(-0.5m, model.TransactionCosts);
    }

    [Fact]
    public void Empty_Or_Null_AmountFields_Default_To_Zero()
    {
        var entity = new TransactionEntity
        {
            RowKey = "rk",
            Date = "",                 // ParseDateOnly => default
            Ticker = "X",
            Amount = "",             // ParseDecimal => 0
            PurchasePrice = "",
            TransactionCosts = "   "
        };

        var model = entity.ToModel();

        Assert.Equal(default, model.Date);
        Assert.Equal(0m, model.Amount);
        Assert.Equal(0m, model.PurchasePrice);
        Assert.Equal(0m, model.TransactionCosts);
    }

    [Fact]
    public void Model_ToEntity_ToModel_Roundtrips_When_RowKey_Provided()
    {
        var original = new Transaction
        {
            RowKey = "rk-provided",
            Date = new DateOnly(2024, 09, 30),
            Ticker = "NVDA",
            Amount = 1.2345678901234567m,
            PurchasePrice = 1000m,
            TransactionCosts = 0.10m
        };

        var entity = original.ToEntity();
        var roundtripped = entity.ToModel();

        Assert.Equal(original.RowKey, roundtripped.RowKey);
        Assert.Equal(original.Date, roundtripped.Date);
        Assert.Equal(original.Ticker, roundtripped.Ticker);
        Assert.Equal(original.Amount, roundtripped.Amount);
        Assert.Equal(original.PurchasePrice, roundtripped.PurchasePrice);
        Assert.Equal(original.TransactionCosts, roundtripped.TransactionCosts);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generates_RowKey_And_Roundtrips_When_RowKey_Missing(string? rowKey)
    {
        var original = new Transaction
        {
            RowKey = rowKey,
            Date = new DateOnly(2024, 03, 15),
            Ticker = "ASML",
            Amount = 42.0m,
            PurchasePrice = 123.45m,
            TransactionCosts = 0m
        };

        var entity = original.ToEntity();
        var roundtripped = entity.ToModel();

        Assert.False(string.IsNullOrWhiteSpace(roundtripped.RowKey));
        Assert.True(Guid.TryParseExact(roundtripped.RowKey, "N", out _));
        Assert.Equal(original.Date, roundtripped.Date);
        Assert.Equal(original.Ticker, roundtripped.Ticker);
        Assert.Equal(original.Amount, roundtripped.Amount);
        Assert.Equal(original.PurchasePrice, roundtripped.PurchasePrice);
        Assert.Equal(original.TransactionCosts, roundtripped.TransactionCosts);
    }

    [Fact]
    public void Default_Date_Roundtrips_As_Default()
    {
        var original = new Transaction
        {
            RowKey = "rk",
            Date = default,
            Ticker = "X",
            Amount = 0m,
            PurchasePrice = 0m,
            TransactionCosts = 0m
        };

        var entity = original.ToEntity();
        var roundtripped = entity.ToModel();

        Assert.Equal(default, roundtripped.Date);
    }
}
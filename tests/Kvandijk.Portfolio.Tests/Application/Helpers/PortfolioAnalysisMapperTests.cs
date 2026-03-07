using Azure;
using Kvandijk.Portfolio.Application.Dtos;
using Kvandijk.Portfolio.Application.Mappers;
using Kvandijk.Portfolio.Domain.Entities;
using Kvandijk.Portfolio.Domain.Utils;

namespace Kvandijk.Portfolio.Tests.Application.Helpers;

public class PortfolioAnalysisMapperTests
{
    [Fact]
    public void ToEntity_MapsAllFields()
    {
        var dto = new PortfolioAnalysisDto
        {
            RowKey = "rk-abc",
            AnalysisDate = new DateOnly(2025, 6, 15),
            AnalysisType = "weekly",
            WeekNumber = 3,
            Content = "Portfolio looking strong"
        };

        var entity = dto.ToEntity("snapshot-json");

        Assert.Equal(StaticDetails.AiAnalysesPartitionKey, entity.PartitionKey);
        Assert.Equal("rk-abc", entity.RowKey);
        Assert.Equal("2025-06-15", entity.AnalysisDate);
        Assert.Equal("weekly", entity.AnalysisType);
        Assert.Equal("3", entity.WeekNumber);
        Assert.Equal("Portfolio looking strong", entity.Content);
        Assert.Equal("snapshot-json", entity.PortfolioSnapshot);
        Assert.Equal(ETag.All, entity.ETag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToEntity_GeneratesRowKeyWhenMissing(string? rowKey)
    {
        var dto = new PortfolioAnalysisDto
        {
            RowKey = rowKey!,
            AnalysisDate = new DateOnly(2025, 1, 1),
            AnalysisType = "weekly",
            WeekNumber = 1,
            Content = "test"
        };

        var entity = dto.ToEntity();

        Assert.False(string.IsNullOrWhiteSpace(entity.RowKey));
        Assert.True(Guid.TryParseExact(entity.RowKey, "N", out _));
    }

    [Fact]
    public void ToEntity_SetsPortfolioSnapshot()
    {
        var dto = new PortfolioAnalysisDto
        {
            RowKey = "rk",
            AnalysisDate = new DateOnly(2025, 1, 1),
            AnalysisType = "weekly",
            WeekNumber = 1,
            Content = "test"
        };

        var withSnapshot = dto.ToEntity("my-snapshot");
        var withoutSnapshot = dto.ToEntity();

        Assert.Equal("my-snapshot", withSnapshot.PortfolioSnapshot);
        Assert.Equal("", withoutSnapshot.PortfolioSnapshot);
    }

    [Fact]
    public void ToDto_MapsAllFields()
    {
        var entity = new PortfolioAnalysisEntity
        {
            PartitionKey = StaticDetails.AiAnalysesPartitionKey,
            RowKey = "rk-xyz",
            AnalysisDate = "2025-06-15",
            AnalysisType = "weekly",
            WeekNumber = "3",
            Content = "Analysis content",
            PortfolioSnapshot = "snapshot",
            ETag = ETag.All
        };

        var dto = entity.ToDto();

        Assert.Equal("rk-xyz", dto.RowKey);
        Assert.Equal(new DateOnly(2025, 6, 15), dto.AnalysisDate);
        Assert.Equal("weekly", dto.AnalysisType);
        Assert.Equal(3, dto.WeekNumber);
        Assert.Equal("Analysis content", dto.Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("not-a-number")]
    public void ToDto_InvalidWeekNumber_DefaultsToZero(string weekNumber)
    {
        var entity = new PortfolioAnalysisEntity
        {
            RowKey = "rk",
            AnalysisDate = "2025-01-01",
            AnalysisType = "weekly",
            WeekNumber = weekNumber,
            Content = "test"
        };

        var dto = entity.ToDto();

        Assert.Equal(0, dto.WeekNumber);
    }

    [Fact]
    public void Roundtrip_PreservesAllValues()
    {
        var original = new PortfolioAnalysisDto
        {
            RowKey = "rk-roundtrip",
            AnalysisDate = new DateOnly(2025, 9, 30),
            AnalysisType = "monthly",
            WeekNumber = 0,
            Content = "Full roundtrip content"
        };

        var entity = original.ToEntity();
        var roundtripped = entity.ToDto();

        Assert.Equal(original.RowKey, roundtripped.RowKey);
        Assert.Equal(original.AnalysisDate, roundtripped.AnalysisDate);
        Assert.Equal(original.AnalysisType, roundtripped.AnalysisType);
        Assert.Equal(original.WeekNumber, roundtripped.WeekNumber);
        Assert.Equal(original.Content, roundtripped.Content);
    }

    [Fact]
    public void Roundtrip_DefaultDate_PreservesAsDefault()
    {
        var original = new PortfolioAnalysisDto
        {
            RowKey = "rk",
            AnalysisDate = default,
            AnalysisType = "weekly",
            WeekNumber = 1,
            Content = "test"
        };

        var entity = original.ToEntity();
        var roundtripped = entity.ToDto();

        Assert.Equal(default, roundtripped.AnalysisDate);
    }
}

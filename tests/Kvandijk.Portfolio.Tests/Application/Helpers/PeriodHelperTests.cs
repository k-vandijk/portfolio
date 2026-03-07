using Kvandijk.Portfolio.Application.Helpers;
using Kvandijk.Portfolio.Domain.Utils;

namespace Kvandijk.Portfolio.Tests.Application.Helpers;

public class PeriodHelperTests
{
    [Fact]
    public void GetDefaultPeriod_WithCustomFirstDate_CalculatesFromThatDate()
    {
        var firstDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);

        var result = PeriodHelper.GetDefaultPeriod(firstDate);

        var expected = $"{30 + StaticDetails.TickerApiBufferDays}d";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDefaultPeriod_NullFirstDate_UsesStaticDetailsDefault()
    {
        var result = PeriodHelper.GetDefaultPeriod(null);

        var firstDate = DateOnly.Parse(StaticDetails.FirstTransactionDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expected = $"{today.DayNumber - firstDate.DayNumber + StaticDetails.TickerApiBufferDays}d";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPeriodFromTimeRange_1W_Returns14d()
    {
        var result = PeriodHelper.GetPeriodFromTimeRange("1W");

        Assert.Equal($"{7 + StaticDetails.TickerApiBufferDays}d", result);
    }

    [Fact]
    public void GetPeriodFromTimeRange_1M_Returns38d()
    {
        var result = PeriodHelper.GetPeriodFromTimeRange("1M");

        Assert.Equal($"{31 + StaticDetails.TickerApiBufferDays}d", result);
    }

    [Fact]
    public void GetPeriodFromTimeRange_3M_Returns100d()
    {
        var result = PeriodHelper.GetPeriodFromTimeRange("3M");

        Assert.Equal($"{31 * 3 + StaticDetails.TickerApiBufferDays}d", result);
    }

    [Fact]
    public void GetPeriodFromTimeRange_YTD_ReturnsCorrectDays()
    {
        var result = PeriodHelper.GetPeriodFromTimeRange("YTD");

        var today = DateTime.UtcNow;
        var startOfYear = new DateTime(today.Year, 1, 1);
        var expected = $"{(today - startOfYear).Days + StaticDetails.TickerApiBufferDays}d";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPeriodFromTimeRange_ALL_DelegatesToGetDefaultPeriod()
    {
        var result = PeriodHelper.GetPeriodFromTimeRange("ALL");

        var expected = PeriodHelper.GetDefaultPeriod();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPeriodFromTimeRange_Null_DelegatesToGetDefaultPeriod()
    {
        var result = PeriodHelper.GetPeriodFromTimeRange(null);

        var expected = PeriodHelper.GetDefaultPeriod();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1w")]
    [InlineData("1W")]
    [InlineData("1m")]
    [InlineData("3m")]
    [InlineData("ytd")]
    [InlineData("all")]
    public void GetPeriodFromTimeRange_CaseInsensitive(string input)
    {
        var result = PeriodHelper.GetPeriodFromTimeRange(input);

        Assert.Matches(@"^\d+d$", result);
    }

    [Fact]
    public void GetPeriodFromYear_ReturnsCorrectDays()
    {
        var year = DateTime.UtcNow.Year;
        var result = PeriodHelper.GetPeriodFromYear(year);

        var startOfYear = new DateOnly(year, 1, 1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expected = $"{today.DayNumber - startOfYear.DayNumber + StaticDetails.TickerApiBufferDays}d";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetYtdPeriod_ReturnsCorrectDays()
    {
        var result = PeriodHelper.GetYtdPeriod();

        var today = DateTime.UtcNow;
        var startOfYear = new DateTime(today.Year, 1, 1);
        var expected = $"{(today - startOfYear).Days + StaticDetails.TickerApiBufferDays}d";
        Assert.Equal(expected, result);
    }
}

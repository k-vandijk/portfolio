using Dashboard.Application.Helpers;

namespace Dashboard.Tests.Application.Helpers;

public class PeriodHelperTests
{
    [Fact]
    public void SameYear_Returns1y()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new DateOnly(today.Year, 1, 1); // any day in current year should yield "1y"

        var period = PeriodHelper.GetDefaultPeriod(first);

        Assert.Equal("1y", period);
    }

    [Fact]
    public void PreviousYear_Returns2y()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new DateOnly(today.Year - 1, 6, 15); // month/day should not matter

        var period = PeriodHelper.GetDefaultPeriod(first);

        Assert.Equal("2y", period); // inclusive of last year + this year
    }

    [Theory]
    [InlineData(0, 1)] // same year -> 1y
    [InlineData(1, 2)] // 1 year ago -> 2y
    [InlineData(5, 6)] // 5 years ago -> 6y
    [InlineData(20, 21)] // 20 years ago -> 21y
    public void NYearsAgo_ReturnsInclusiveCount(int yearsAgo, int expectedYears)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new DateOnly(today.Year - yearsAgo, 12, 31);

        var period = PeriodHelper.GetDefaultPeriod(first);

        Assert.Equal($"{expectedYears}y", period);
    }

    [Fact]
    public void OutputFormat_IsNumberFollowedByLowercaseY()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new DateOnly(today.Year, today.Month, Math.Min(today.Day, 28)); // safe day

        var period = PeriodHelper.GetDefaultPeriod(first);

        Assert.Matches(@"^\d+y$", period);
    }

    [Theory]
    [InlineData("1W", "7d")]
    [InlineData("1w", "7d")]
    [InlineData("1M", "1mo")]
    [InlineData("1m", "1mo")]
    [InlineData("3M", "3mo")]
    [InlineData("3m", "3mo")]
    [InlineData("YTD", "ytd")]
    [InlineData("ytd", "ytd")]
    [InlineData("ALL", null)] // Will use default period calculation (ny format)
    [InlineData("all", null)]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("unknown", null)]
    public void GetPeriodFromTimeRange_ReturnsCorrectPeriod(string? timerange, string? expectedPeriod)
    {
        var result = PeriodHelper.GetPeriodFromTimeRange(timerange);

        if (expectedPeriod != null)
        {
            Assert.Equal(expectedPeriod, result);
        }
        else
        {
            // For ALL, null, or unknown values, should return default period (ny format where n is a digit)
            Assert.Matches(@"^\d+y$", result);
        }
    }

    [Fact]
    public void GetPeriodFromYear_CurrentYear_Returns2y()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentYear = today.Year;

        var result = PeriodHelper.GetPeriodFromYear(currentYear);

        Assert.Equal("2y", result);
    }

    [Fact]
    public void GetPeriodFromYear_LastYear_Returns3y()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lastYear = today.Year - 1;

        var result = PeriodHelper.GetPeriodFromYear(lastYear);

        Assert.Equal("3y", result);
    }

    [Theory]
    [InlineData(0, 2)]  // current year -> 2y (includes buffer)
    [InlineData(1, 3)]  // 1 year ago -> 3y
    [InlineData(2, 4)]  // 2 years ago -> 4y
    public void GetPeriodFromYear_VariousYears_ReturnsCorrectPeriod(int yearsAgo, int expectedYears)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Year - yearsAgo;

        var result = PeriodHelper.GetPeriodFromYear(year);

        Assert.Equal($"{expectedYears}y", result);
    }
}
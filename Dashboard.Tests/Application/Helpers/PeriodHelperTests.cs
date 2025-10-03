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
}
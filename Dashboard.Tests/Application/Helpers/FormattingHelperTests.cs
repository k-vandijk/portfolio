using System.Globalization;
using Dashboard.Application.Helpers;

namespace Dashboard.Tests.Application.Helpers;

public class FormattingHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsZero_ForNullOrWhitespace(string? input)
    {
        var result = FormattingHelper.ParseDecimal(input);
        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData("123.45", 123.45)]
    [InlineData("-0.5", -0.5)]
    [InlineData("+42.0", 42.0)]
    [InlineData("1000", 1000)]
    public void ParsesInvariant_DecimalsFirst(string input, decimal expected)
    {
        // Uses CultureInfo.InvariantCulture with decimal point and leading sign only.
        var result = FormattingHelper.ParseDecimal(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1234,56", 1234.56)]      // comma decimal (nl-NL)
    [InlineData("1.234,56", 1234.56)]     // thousands '.' + decimal ','
    [InlineData("-1.234,56", -1234.56)]
    [InlineData("1.234", 1.234)]          // '.' as decimal in InvariantCulture
    [InlineData("1,234", 1.234)]          // ',' as decimal (nl-NL)
    public void FallsBack_ToNlNl_IfInvariantFails(string input, decimal expected)
    {
        var result = FormattingHelper.ParseDecimal(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1,234.56")]  // US-style thousands + decimal — not supported by either path
    [InlineData("abc")]
    [InlineData("12,34,56")]
    public void ReturnsZero_WhenNoCultureMatches(string input)
    {
        var result = FormattingHelper.ParseDecimal(input);
        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsDefault_ForNullOrWhitespace(string? input)
    {
        var result = FormattingHelper.ParseDateOnly(input);
        Assert.Equal(default, result);
    }

    [Theory]
    [InlineData("2024-05-17", 2024, 05, 17)] // ISO invariant (DateOnly)
    [InlineData("05/17/2024", 2024, 05, 17)] // Invariant-style DateOnly/DateTime pattern
    [InlineData("2024-05-17T15:30:45", 2024, 05, 17)] // DateTime → DateOnly
    [InlineData("2024-05-17T23:59:59Z", 2024, 05, 17)] // With 'Z' (UTC) → DateOnly
    public void Parses_Invariant_DateOrDateTime_ThenCoercesToDateOnly(string input, int year, int month, int day)
    {
        var result = FormattingHelper.ParseDateOnly(input);
        Assert.Equal(new DateOnly(year, month, day), result);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("32/13/2024")] // impossible date
    public void ReturnsDefault_OnInvalid(string input)
    {
        var result = FormattingHelper.ParseDateOnly(input);
        Assert.Equal(default, result);
    }

    [Fact]
    public void ReturnsEmptyString_ForDefaultDate()
    {
        var result = FormattingHelper.FormatDate(default);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Formats_AsIsoYyyyMmDd_Invariant()
    {
        var d = new DateOnly(2024, 5, 9);
        var result = FormattingHelper.FormatDate(d);
        Assert.Equal("2024-05-09", result);
    }
    [Theory]
    [InlineData(0, "0")]
    [InlineData(12.34, "12.34")]
    [InlineData(-0.5, "-0.5")]
    [InlineData(1000, "1000")] // no thousands separator
    public void Formats_Invariant_NoThousands_NoTrailingZeros(double value, string expected)
    {
        var dec = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        var result = FormattingHelper.FormatDecimal(dec);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Trims_TrailingZeros()
    {
        var result = FormattingHelper.FormatDecimal(1.2300m);
        Assert.Equal("1.23", result);
    }

    [Fact]
    public void Supports_UpTo16FractionalDigits()
    {
        var value = 1.2345678901234567m; // 16 fractional digits
        var result = FormattingHelper.FormatDecimal(value);
        Assert.Equal("1.2345678901234567", result);
    }
}
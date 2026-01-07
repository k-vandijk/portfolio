using System.Globalization;

namespace Dashboard.Application.Helpers;

public static class FormattingHelper
{
    public static decimal ParseDecimal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0m;

        // We slaan op met InvariantCulture, dus eerst (en eigenlijk: uitsluitend) zo parsen
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var inv))
            return inv;

        // Optionele fallback naar nl-NL voor oude/handmatig ingevoerde data met komma
        if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign, CultureInfo.GetCultureInfo("nl-NL"), out var nl))
            return nl;

        return 0m;
    }

    public static DateOnly ParseDateOnly(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return default;

        if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        // Use RoundtripKind to properly handle UTC indicators like 'Z'
        // This prevents timezone conversion issues when parsing UTC timestamps
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return DateOnly.FromDateTime(dt);

        return default; // or throw new FormatException($"Invalid date: {input}");
    }

    public static string FormatDate(DateOnly d) =>
        d == default ? "" : d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FormatDecimal(decimal d) =>
        d.ToString("0.################", CultureInfo.InvariantCulture);
}
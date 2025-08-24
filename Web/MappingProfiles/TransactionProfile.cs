using AutoMapper;
using System.Globalization;
using Web.Models;

namespace Web.MappingProfiles;

public class TransactionProfile : Profile
{
    private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("nl-NL");

    public TransactionProfile()
    {
        CreateMap<TransactionEntity, Transaction>()
            .ForMember(d => d.Date, o => o.MapFrom(s => ParseDateOnly(s.Date)))
            .ForMember(d => d.Ticker, o => o.MapFrom(s => s.Ticker ?? string.Empty))
            .ForMember(d => d.Amount, o => o.MapFrom(s => ParseDecimal(s.Amount)))
            .ForMember(d => d.PurchasePrice, o => o.MapFrom(s => ParseDecimal(s.PurchasePrice)))
            .ForMember(d => d.TransactionCosts, o => o.MapFrom(s => ParseDecimal(s.TransactionCosts)));
    }

    private static decimal ParseDecimal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0m;

        if (decimal.TryParse(input, NumberStyles.Any, NumberCulture, out var result))
            return result;

        // fallback to InvariantCulture (e.g. dot decimals)
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return result;

        return 0m; // or throw new FormatException($"Invalid decimal: {input}");
    }

    private static DateOnly ParseDateOnly(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return default;

        if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateOnly.FromDateTime(dt);

        return default; // or throw new FormatException($"Invalid date: {input}");
    }
}
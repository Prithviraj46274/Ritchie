using System.Globalization;

namespace Richie.Application.Common;

/// <summary>
/// Central money formatter — prefixes the Indian Rupee symbol (₹) and uses Indian digit grouping
/// (lakhs, crores format). Single source of truth so every on-screen amount looks the same.
/// </summary>
public static class CurrencyFormatter
{
    public const string Symbol = "₹";
    private static readonly CultureInfo IndianCulture = new("en-IN");

    /// <summary>₹ with two decimals, Indian format, e.g. "₹1,53,000.00".</summary>
    public static string Format(decimal value) => Symbol + value.ToString("N2", IndianCulture);

    /// <summary>₹ with no decimals, Indian format, e.g. "₹1,53,000".</summary>
    public static string FormatWhole(decimal value) => Symbol + value.ToString("N0", IndianCulture);

    /// <summary>₹ with custom decimal places, Indian format.</summary>
    public static string Format(decimal value, int decimalPlaces) => Symbol + value.ToString($"N{decimalPlaces}", IndianCulture);
}


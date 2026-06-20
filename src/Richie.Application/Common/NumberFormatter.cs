using System.Globalization;

namespace Richie.Application.Common;

/// <summary>
/// Indian number formatter with proper comma placement (lakhs, crores).
/// Format: 1,00,000 (one lakh), 10,00,000 (ten lakhs), 1,00,00,000 (one crore)
/// </summary>
public static class NumberFormatter
{
    private static readonly CultureInfo IndianCulture = new("en-IN");

    /// <summary>
    /// Format number with Indian comma grouping (e.g., 1,00,000.00)
    /// </summary>
    public static string FormatIndian(decimal value) => value.ToString("N2", IndianCulture);

    /// <summary>
    /// Format number with Indian comma grouping, no decimals (e.g., 1,00,000)
    /// </summary>
    public static string FormatIndianWhole(decimal value) => value.ToString("N0", IndianCulture);

    /// <summary>
    /// Format number with Indian comma grouping, custom decimal places
    /// </summary>
    public static string FormatIndian(decimal value, int decimalPlaces) => value.ToString($"N{decimalPlaces}", IndianCulture);
}

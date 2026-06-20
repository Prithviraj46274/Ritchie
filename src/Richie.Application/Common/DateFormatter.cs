using System.Globalization;

namespace Richie.Application.Common;

/// <summary>
/// Standard date formatter using DD-MM-YYYY format across the application.
/// </summary>
public static class DateFormatter
{
    private const string StandardFormat = "dd-MM-yyyy";

    /// <summary>
    /// Format date as DD-MM-YYYY
    /// </summary>
    public static string Format(DateTime date) => date.ToString(StandardFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// Format nullable date as DD-MM-YYYY, returns empty string if null
    /// </summary>
    public static string Format(DateTime? date) => date?.ToString(StandardFormat, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Parse date from DD-MM-YYYY format
    /// </summary>
    public static bool TryParse(string value, out DateTime date) =>
        DateTime.TryParseExact(value, StandardFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}

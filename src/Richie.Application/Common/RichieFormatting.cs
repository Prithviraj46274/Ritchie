using System;
using System.Globalization;

namespace Richie.Application.Common;

/// <summary>
/// Central formatting utilities for the WPF UI.
/// Goals:
/// - Numbers: never use K/M compact notation; use full Indian-style grouping.
/// - Dates: always render as DD-MM-YYYY.
/// </summary>
public static class RichieFormatting
{
    /// <summary>
    /// Culture used for Indian digit grouping.
    /// </summary>
    public static readonly CultureInfo IndianCulture = new CultureInfo("en-IN");

    public static string FormatDate(DateTime value) => value.ToString("dd-MM-yyyy", IndianCulture);

    public static string FormatDate(DateTime? value) => value is null ? string.Empty : FormatDate(value.Value);

    public static string FormatChartValue(double value)
    {
        // For chart labels: show full Indian grouping with 0 decimals for integers, 2 otherwise.
        int decimals = Math.Abs(value - Math.Round(value)) < 0.00001 ? 0 : 2;
        string format = decimals == 0 ? "N0" : $"N{decimals}";
        return value.ToString(format, IndianCulture);
    }

    public static string FormatNumber(decimal value, int decimals = 2)
    {
        string format = decimals == 0 ? "N0" : $"N{decimals}";
        return value.ToString(format, IndianCulture);
    }
}


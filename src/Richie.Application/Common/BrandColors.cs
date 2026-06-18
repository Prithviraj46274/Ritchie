namespace Richie.Application.Common;

/// <summary>
/// The central Richie brand palette (adapted from the design system). Values are sRGB hex strings so
/// any layer can convert to its own colour type (WPF <c>Media.Color</c>, SkiaSharp <c>SKColor</c>).
/// The categorical list is colour-blind-safe and is the single source of truth for chart series
/// colours on-screen and in exported reports. No "Others" bucket — every series is named explicitly.
/// Premium warm palette: Soft Golden Orange accent, professional chart colors, accessible contrasts.
/// </summary>
public static class BrandColors
{
    // PRIMARY BRAND COLORS (Premium Golden Orange + Green)
    public const string Primary = "#E6A756";      // Soft Golden Orange (primary accent)
    public const string PrimaryHover = "#D99F3E"; // Darker orange on hover
    public const string PrimaryPressed = "#C98D2D"; // Pressed state

    // STATUS TRIAD (Consistent app-wide): green = good, orange = moderate, red = critical
    public const string Success = "#57B894";      // Professional green - healthy/good
    public const string Warning = "#E6A756";      // Soft golden orange - moderate/caution
    public const string Danger = "#D96C6C";       // Professional red - critical/loss

    // RESERVED COLORS for profit (green) / loss (red) in exported reports
    // By design: NO chart series or other report component uses these two colours
    // This keeps them semantically unambiguous.
    public const string ProfitGreen = "#57B894";  // Green profit
    public const string LossRed = "#D96C6C";      // Red loss (no bright orange)

    /// <summary>
    /// Professional chart palette for reports — carefully selected to be:
    /// - Distinct and visually balanced
    /// - Free of pure red/green (reserved for profit/loss)
    /// - Accessible for mild color blindness
    /// - Modern and premium appearance
    /// </summary>
    public static readonly IReadOnlyList<string> ReportChartPalette =
    [
        "#5B8DEF", // Equity: Professional Blue
        "#E6A756", // Mutual Funds: Soft Golden Orange
        "#56B7B1", // Real Estate: Teal
        "#9B7EDE", // Digital Gold: Purple
        "#F3C969", // Gold Jewellery: Light Gold
        "#8A8A8A", // Guaranteed Plans: Gray
        "#4FA87A", // Secondary: Emerald
        "#2A7DB1", // Secondary: Ocean
        "#CC7DAC", // Secondary: Rose
        "#B8860B"  // Secondary: Dark Goldenrod
    ];

    /// <summary>
    /// Distinct, color-blind-safe categorical series colors for asset allocation charts.
    /// Using the premium palette with proper contrast and professional appearance.
    /// </summary>
    public static readonly IReadOnlyList<string> Categorical =
    [
        "#5B8DEF", // Equity: Professional Blue
        "#E6A756", // Mutual Funds: Soft Golden Orange
        "#56B7B1", // Real Estate: Teal
        "#9B7EDE", // Digital Gold: Purple
        "#F3C969", // Gold Jewellery: Light Gold
        "#8A8A8A", // Guaranteed Plans: Gray
        "#4FA87A", // Emerald (secondary)
        "#2A7DB1", // Ocean Blue (secondary)
        "#CC7DAC", // Rose (secondary)
        "#B8860B"  // Dark Goldenrod (secondary)
    ];
}

using LiveChartsCore.SkiaSharpView.Painting;
using Richie.Application.Common;
using SkiaSharp;

namespace Richie.UI.Charts;

/// <summary>
/// Brand chart colours for on-screen LiveCharts series, backed by <see cref="BrandColors"/> so the
/// in-app charts match the exported report charts. Categorical for pies (one colour per slice),
/// solid brand colours for single-series columns/lines.
/// </summary>
public static class BrandPalette
{
    public static bool IsDarkMode => Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;

    private static readonly SKColor[] LightColors = BrandColors.Categorical.Select(SKColor.Parse).ToArray();
    private static readonly SKColor[] DarkColors = [
        SKColor.Parse("#818CF8"), // Indigo
        SKColor.Parse("#F59E0B"), // Amber
        SKColor.Parse("#A78BFA"), // Violet
        SKColor.Parse("#FB923C"), // Orange
        SKColor.Parse("#38BDF8"), // Sky Blue
        SKColor.Parse("#C084FC"), // Purple
        SKColor.Parse("#FDE047"), // Yellow
        SKColor.Parse("#93C5FD"), // Soft Blue
        SKColor.Parse("#94A3B8"), // Slate
        SKColor.Parse("#6366F1")  // Indigo Blue
    ];

    public static SKColor[] Colors => IsDarkMode ? DarkColors : LightColors;

    public static SKColor Primary => IsDarkMode ? SKColor.Parse("#818CF8") : SKColor.Parse(BrandColors.Primary);
    public static SKColor Success => IsDarkMode ? SKColor.Parse("#2DD4BF") : SKColor.Parse(BrandColors.Success);
    public static SKColor Warning => IsDarkMode ? SKColor.Parse("#F59E0B") : SKColor.Parse(BrandColors.Warning);
    public static SKColor Danger => IsDarkMode ? SKColor.Parse("#EF4444") : SKColor.Parse(BrandColors.Danger);

    public static SolidColorPaint ChartAxesLabelPaint => IsDarkMode
        ? new SolidColorPaint(SKColor.Parse("#FFFFFF")) { SKTypeface = SKTypeface.FromFamilyName("Arial") } // Pure White
        : new SolidColorPaint(SKColor.Parse("#475569")) { SKTypeface = SKTypeface.FromFamilyName("Arial") }; // Dark slate

    public static SolidColorPaint ChartGridLinesPaint => IsDarkMode
        ? new SolidColorPaint(SKColor.Parse("#334155")) { StrokeThickness = 1 } // Dark gridline
        : new SolidColorPaint(SKColor.Parse("#E2E8F0")) { StrokeThickness = 1 }; // Light gridline

    public static SKColor At(int index) => Colors[index % Colors.Length];

    /// <summary>The categorical colour at <paramref name="index"/> as a frozen WPF brush — for
    /// custom chart legends that must match the on-chart slice colours exactly.</summary>
    public static System.Windows.Media.Brush MediaBrush(int index)
    {
        SKColor c = At(index);
        var brush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue));
        brush.Freeze();
        return brush;
    }

    /// <summary>A fresh paint for the categorical colour at <paramref name="index"/> (wraps around).</summary>
    public static SolidColorPaint Categorical(int index) => new(At(index));

    /// <summary>A fresh paint for a specific brand colour.</summary>
    public static SolidColorPaint Solid(SKColor color) => new(color);
}

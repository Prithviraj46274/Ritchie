using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Richie.UI.Converters;

/// <summary>Colours a profit/loss value: green when ≥ 0, light orange when negative (app-wide status palette).</summary>
public sealed class ProfitLossBrushConverter : IValueConverter
{
    private static readonly Brush Green = Freeze(Color.FromRgb(0x0F, 0x7B, 0x0F));
    private static readonly Brush Red = Freeze(Color.FromRgb(0xC4, 0x2B, 0x1C));
    private static readonly Brush DarkGreen = Freeze(Color.FromRgb(0x22, 0xC5, 0x5E));
    private static readonly Brush DarkRed = Freeze(Color.FromRgb(0xEF, 0x44, 0x44));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double n = value switch
        {
            decimal d => (double)d,
            double db => db,
            float f => f,
            int i => i,
            _ => 0
        };

        bool isDark = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;
        if (isDark)
        {
            return n < 0 ? DarkRed : DarkGreen;
        }

        return n < 0 ? Red : Green;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Richie.UI.Converters;

/// <summary>Colours a profit/loss value: green when ≥ 0, red when negative (app-wide status palette).</summary>
public sealed class ProfitLossBrushConverter : IValueConverter
{
    private static readonly Brush Green = Freeze(Color.FromRgb(0x0F, 0x7B, 0x0F));
    private static readonly Brush Red = Freeze(Color.FromRgb(0xC4, 0x2B, 0x1C));

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

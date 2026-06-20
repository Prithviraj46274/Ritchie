using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Richie.Application.Common;
using Richie.Application.Reports;
using SkiaSharp;

namespace Richie.Infrastructure.Reports;

public sealed partial class ReportExporter
{
    // Larger chart canvas for better readability in PDF.
    private const int ChartWidth  = 1400;
    private const int ChartHeight = 700;

    private static readonly SKColor LabelColor       = new(0x1A, 0x29, 0x42); // Navy
    private static readonly SKColor LabelColorLight  = new(0xFF, 0xFF, 0xFF); // White (for dark bg)
    private static readonly SKColor GridLineColor    = new(0xE0, 0xE8, 0xF0); // Soft blue-grey grid
    private static readonly SKColor AxisColor        = new(0x5A, 0x72, 0x88); // Muted axis text

    // Report-chart palette deliberately excludes green and red so those stay reserved for the
    // profit/loss colouring elsewhere in the report. Explicit so slices/bars are visible without a
    // configured LiveCharts theme.
    private static readonly SKColor[] Palette =
        BrandColors.ReportChartPalette.Select(SKColor.Parse).ToArray();

    /// <summary>Renders a report chart spec to a PNG image using SkiaSharp in-memory charts (no WPF).</summary>
    public static byte[] RenderChartImage(ReportChart chart)
    {
        InMemorySkiaSharpChart view = chart.Kind switch
        {
            ReportChartKind.Donut  => BuildDonut(chart.Points),
            ReportChartKind.Pie    => BuildPie(chart.Points),
            _                      => BuildColumn(chart.Points)
        };

        using var stream = new MemoryStream();
        view.SaveImage(stream);
        return stream.ToArray();
    }

    // ── Donut chart (allocation — most important visual) ─────────────────────

    private static SKPieChart BuildDonut(IReadOnlyList<ReportChartPoint> points)
    {
        var series = new List<ISeries>(points.Count);
        double total = points.Sum(p => p.Value);

        for (int i = 0; i < points.Count; i++)
        {
            ReportChartPoint pt = points[i];
            double pct = total > 0 ? pt.Value / total * 100.0 : 0;
            series.Add(new PieSeries<double>
            {
                Values                  = [pt.Value],
                Name                    = pt.Label,
                Fill                    = new SolidColorPaint(Palette[i % Palette.Length]),
                Stroke                  = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                DataLabelsPaint         = new SolidColorPaint(LabelColor),
                DataLabelsFormatter     = ctx => $"{pt.Label}  {pct:0.#}%",
                DataLabelsPosition      = PolarLabelsPosition.Outer,
                DataLabelsSize          = 15,
                InnerRadius             = 0.55,          // donut hole
            });
        }

        return new SKPieChart
        {
            Width      = ChartWidth,
            Height     = ChartHeight,
            Background = SKColors.White,
            Series     = series
        };
    }

    // ── Standard pie chart ────────────────────────────────────────────────────

    private static SKPieChart BuildPie(IReadOnlyList<ReportChartPoint> points)
    {
        double total = points.Sum(p => p.Value);
        var series = new List<ISeries>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            ReportChartPoint pt = points[i];
            double pct = total > 0 ? pt.Value / total * 100.0 : 0;
            series.Add(new PieSeries<double>
            {
                Values              = [pt.Value],
                Name                = pt.Label,
                Fill                = new SolidColorPaint(Palette[i % Palette.Length]),
                Stroke              = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                DataLabelsPaint     = new SolidColorPaint(LabelColor),
                DataLabelsFormatter = _ => $"{pt.Label}  {pct:0.#}%",
                DataLabelsPosition  = PolarLabelsPosition.Outer,
                DataLabelsSize      = 14,
            });
        }

        return new SKPieChart
        {
            Width      = ChartWidth,
            Height     = ChartHeight,
            Background = SKColors.White,
            Series     = series
        };
    }

    // ── Column / bar chart ────────────────────────────────────────────────────

    private static SKCartesianChart BuildColumn(IReadOnlyList<ReportChartPoint> points)
    {
        var column = new ColumnSeries<double>
        {
            Values  = points.Select(p => p.Value).ToArray(),
            Name    = string.Empty,
            Fill    = new SolidColorPaint(Palette[0]),
            Stroke  = new SolidColorPaint(Palette[1]) { StrokeThickness = 0 },
            MaxBarWidth = 40,
            DataLabelsPaint    = new SolidColorPaint(LabelColor),
            DataLabelsFormatter = ctx => $"₹{ctx.Model:N0}",
            DataLabelsPosition  = DataLabelsPosition.Top,
            DataLabelsSize      = 13,
        };

        return new SKCartesianChart
        {
            Width      = ChartWidth,
            Height     = ChartHeight,
            Background = SKColors.White,
            Series     = [column],
            XAxes =
            [
                new Axis
                {
                    Labels          = points.Select(p => p.Label).ToArray(),
                    LabelsPaint     = new SolidColorPaint(AxisColor),
                    LabelsRotation  = -35,
                    TextSize        = 14,
                    SeparatorsPaint = new SolidColorPaint(GridLineColor),
                }
            ],
            YAxes =
            [
                new Axis
                {
                    LabelsPaint     = new SolidColorPaint(AxisColor),
                    TextSize        = 13,
                    SeparatorsPaint = new SolidColorPaint(GridLineColor),
                    Labeler         = value => $"₹{value:N0}",
                }
            ]
        };
    }
}

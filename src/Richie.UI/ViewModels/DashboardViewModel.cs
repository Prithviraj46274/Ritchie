using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Measure;
using Richie.Application.Assets;
using Richie.Application.Audit;
using Richie.Application.Authentication;
using Richie.Application.Dashboard;
using Richie.Application.Expenses;
using Richie.Application.Income;
using Richie.UI.Charts;
using SkiaSharp;

namespace Richie.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboard;
    private readonly IAssetService _assets;
    private readonly IExpenseAnalyticsService _analytics;
    private readonly IIncomeService _income;
    private readonly IUserSession _session;

    public sealed record UpcomingSipRow(string AssetName, string AmountText, string DueText, string FrequencyText);
    public sealed record ActivityRow(string DateText, string Module, string Action, string Description);
    public sealed record InsightRow(string Text, string ActionLabel, InsightTopic Topic);
    public sealed record AllocationLegendItem(string Label, Brush Swatch);

    [ObservableProperty] private string _totalAssetsText = "—";
    [ObservableProperty] private string _totalInvestedText = "—";
    [ObservableProperty] private string _totalExpensesText = "—";
    [ObservableProperty] private string _profitLossText = "—";
    [ObservableProperty] private Brush _profitLossBrush = Brushes.Gray;
    [ObservableProperty] private int _healthScore;
    [ObservableProperty] private string _healthScoreText = string.Empty;
    [ObservableProperty] private string _healthRating = string.Empty;
    [ObservableProperty] private Brush _healthBrush = Brushes.Gray;
    [ObservableProperty] private bool _healthIsInterim;

    [ObservableProperty] private ISeries[] _allocationSeries = [];
    [ObservableProperty] private ObservableCollection<AllocationLegendItem> _allocationLegend = [];
    [ObservableProperty] private bool _hasAssets;
    [ObservableProperty] private bool _noAssets;

    [ObservableProperty] private ISeries[] _incomeExpenseSeries = [];
    [ObservableProperty] private Axis[] _incomeExpenseAxes = [];
    [ObservableProperty] private Axis[] _incomeExpenseYAxes = [];
    [ObservableProperty] private ISeries[] _investmentSeries = [];
    [ObservableProperty] private Axis[] _investmentAxes = [];
    [ObservableProperty] private Axis[] _investmentYAxes = [];
    [ObservableProperty] private string _investmentGrowthText = string.Empty;
    [ObservableProperty] private ISeries[] _expenseBreakdownSeries = [];
    [ObservableProperty] private Axis[] _expenseBreakdownAxes = [];
    [ObservableProperty] private Axis[] _expenseBreakdownYAxes = [];

    // Hero greeting (top of the dashboard).
    [ObservableProperty] private string _heroDateText = string.Empty;
    [ObservableProperty] private string _greetingText = string.Empty;
    [ObservableProperty] private string _portfolioInsightText = string.Empty;

    [ObservableProperty] private ObservableCollection<UpcomingSipRow> _upcomingSips = [];
    [ObservableProperty] private bool _hasUpcomingSips;
    [ObservableProperty] private bool _noUpcomingSips;
    [ObservableProperty] private ObservableCollection<InsightRow> _insights = [];
    [ObservableProperty] private bool _noInsights;
    [ObservableProperty] private ObservableCollection<ActivityRow> _recentActivity = [];
    [ObservableProperty] private bool _hasActivity;
    [ObservableProperty] private bool _noActivity;

private static bool IsDarkMode =>
    Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() ==
    Wpf.Ui.Appearance.ApplicationTheme.Dark;

private static Brush Red => IsDarkMode
    ? new SolidColorBrush(Color.FromRgb(0xD9, 0x6C, 0x6C))   // Soft red in dark mode
    : new SolidColorBrush(Color.FromRgb(0xD9, 0x6C, 0x6C));  // Soft red in light mode

private static Brush Orange => IsDarkMode
    ? new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x56))   // Golden orange
    : new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x56));

private static Brush Green => IsDarkMode
    ? new SolidColorBrush(Color.FromRgb(0x57, 0xB8, 0x94))   // Professional green
    : new SolidColorBrush(Color.FromRgb(0x57, 0xB8, 0x94));

    public DashboardViewModel(IDashboardService dashboard, IAssetService assets,
        IExpenseAnalyticsService analytics, IIncomeService income, IUserSession session)
    {
        _dashboard = dashboard;
        _assets = assets;
        _analytics = analytics;
        _income = income;
        _session = session;
    }

    public void Load()
    {
        DashboardSummary s = _dashboard.GetSummary();

        BuildHero(s);

        TotalAssetsText = CompactMoney(s.TotalAssets);
        TotalInvestedText = CompactMoney(s.TotalInvested);
        TotalExpensesText = CompactMoney(s.TotalExpensesThisMonth);
        ProfitLossText = $"{CompactMoney(s.ProfitLoss)} ({s.ProfitLossPercent:+0.0;-0.0;0.0}%)";
        ProfitLossBrush = s.ProfitLoss < 0 ? Red : Green;
        HealthScore = s.HealthScore;
        HealthScoreText = $"{s.HealthScore}/100";
        HealthRating = s.HealthRating;
        HealthBrush = s.HealthScore >= 80 ? Green : s.HealthScore >= 60 ? Orange : Red;
        HealthIsInterim = s.HealthIsInterim;

        UpcomingSips = new ObservableCollection<UpcomingSipRow>(s.UpcomingSips.Select(u =>
            new UpcomingSipRow(u.AssetName, Money(u.Amount), u.DueDate.ToString("d", CultureInfo.CurrentCulture), u.Frequency.ToString())));
        HasUpcomingSips = UpcomingSips.Count > 0;
        NoUpcomingSips = !HasUpcomingSips;

        // Keep the dashboard focused — show the top few insights horizontally; the rest live on each module.
        Insights = new ObservableCollection<InsightRow>(
            s.Insights.Take(3).Select(i => new InsightRow(i.Text, ActionLabel(i.Topic), i.Topic)));
        NoInsights = Insights.Count == 0;

        RecentActivity = new ObservableCollection<ActivityRow>(s.RecentActivity.Select(a =>
            new ActivityRow(a.TimestampUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture), a.Module, a.Action, a.Description)));
        HasActivity = RecentActivity.Count > 0;
        NoActivity = !HasActivity;

        BuildCharts(s);
    }

    private void BuildCharts(DashboardSummary s)
    {
        // Asset allocation — donut with the share shown in the legend (e.g. "Equity 24%").
        IReadOnlyList<AllocationSlice> allocation = _assets.GetPortfolioSummary().Allocation;
        HasAssets = allocation.Count > 0;
        NoAssets = !HasAssets;
        AllocationSeries = allocation
            .Select((a, i) => (ISeries)new PieSeries<double>
                { Values = new[] { (double)a.Value }, Name = a.TypeName, InnerRadius = 45, Fill = BrandPalette.Categorical(i) })
            .ToArray();
        // Custom legend (matches slice colours) so it lays out inside the card instead of the
        // built-in legend overflowing it.
        AllocationLegend = new ObservableCollection<AllocationLegendItem>(
            allocation.Select((a, i) => new AllocationLegendItem($"{a.TypeName}  {a.Percent:0.#}%", BrandPalette.MediaBrush(i))));
        // Income vs Expense — filled area trend over the last 9 months.
        IReadOnlyList<PeriodDatum> income = _income.GetMonthlyTotals(9);
        IReadOnlyList<PeriodDatum> expense = _analytics.GetMonthlyTotals(9);
        IncomeExpenseSeries = [Area("Income", income, BrandPalette.Success), Area("Expense", expense, BrandPalette.Danger)];
        IncomeExpenseAxes = [new Axis { Labels = income.Select(d => d.Label).ToArray(), LabelsRotation = 0, LabelsPaint = BrandPalette.ChartAxesLabelPaint, SeparatorsPaint = BrandPalette.ChartGridLinesPaint }];
        IncomeExpenseYAxes = [new Axis { LabelsPaint = BrandPalette.ChartAxesLabelPaint, SeparatorsPaint = BrandPalette.ChartGridLinesPaint }];

        // Investment growth — invested capital over time (line + period-growth badge).
        InvestmentSeries =
        [
            new LineSeries<double>
            {
                Name = "Invested",
                Values = s.InvestedHistory.Select(d => (double)d.Amount).ToArray(),
                Stroke = new SolidColorPaint(BrandPalette.Primary) { StrokeThickness = 2.5f },
                GeometryStroke = new SolidColorPaint(BrandPalette.Primary) { StrokeThickness = 2f },
                GeometryFill = new SolidColorPaint(BrandPalette.Primary),
                GeometrySize = 7,
                Fill = null,
                LineSmoothness = 0.5
            }
        ];
        InvestmentAxes = [new Axis { Labels = s.InvestedHistory.Select(d => d.Label).ToArray(), LabelsPaint = BrandPalette.ChartAxesLabelPaint, SeparatorsPaint = BrandPalette.ChartGridLinesPaint }];
        InvestmentYAxes = [new Axis { LabelsPaint = BrandPalette.ChartAxesLabelPaint, SeparatorsPaint = BrandPalette.ChartGridLinesPaint }];
        InvestmentGrowthText = $"{(s.InvestedGrowthPercent >= 0 ? "▲ +" : "▼ ")}{s.InvestedGrowthPercent:0.#}% YoY";

        // Expense breakdown — this month's spend by category (horizontal bar chart for better space efficiency).
        var categories = _analytics.GetCategoryDistribution(AnalyticsPeriod.ThisMonth)
            .Where(c => c.Amount > 0)
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Category-specific colors for each expense type
        var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Transportation"] = "#F4B400",  // Yellow
            ["Food"] = "#34A853",            // Green
            ["Utilities"] = "#4285F4",       // Blue
            ["Shopping"] = "#A142F4",        // Purple
            ["Healthcare"] = "#00ACC1",      // Teal
            ["Entertainment"] = "#FB8C00",   // Orange
            ["Education"] = "#4B4FC7",       // Indigo
            ["Other"] = "#9E9E9E",           // Gray
        };

        // Create one RowSeries per category with its own color (horizontal bars)
        var rowSeries = new List<ISeries>();
        var categoryLabels = new List<string>();
        
        for (int i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            categoryLabels.Add(category.CategoryName);
            
            var color = colorMap.ContainsKey(category.CategoryName)
                ? colorMap[category.CategoryName]
                : "#9E9E9E";
            
            var skColor = SKColor.Parse(color);
            
            // Pad the values array so the value is at the correct index for this category
            var values = new double?[categories.Count];
            values[i] = (double)category.Amount;
            
            rowSeries.Add(new RowSeries<double?>
            {
                Name = category.CategoryName,
                Values = values,
                Fill = new SolidColorPaint(skColor),
                MaxBarWidth = 24,
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = (point) => FormatCompactNumber(point.Coordinate.PrimaryValue)
            });
        }

        ExpenseBreakdownSeries = rowSeries.ToArray();
        
        // X axis: values (horizontal) — compact formatting, start at zero
        ExpenseBreakdownAxes = [new Axis { Labeler = FormatCompactNumber, MinLimit = 0, LabelsPaint = BrandPalette.ChartAxesLabelPaint, SeparatorsPaint = BrandPalette.ChartGridLinesPaint }];
        
        // Y axis: category labels (vertical)
        ExpenseBreakdownYAxes = [new Axis { Labels = categoryLabels.ToArray(), LabelsPaint = BrandPalette.ChartAxesLabelPaint, SeparatorsPaint = BrandPalette.ChartGridLinesPaint }];
    }

    private static LineSeries<double> Area(string name, IReadOnlyList<PeriodDatum> data, SKColor color) => new()
    {
        Name = name,
        Values = data.Select(d => (double)d.Amount).ToArray(),
        Stroke = new SolidColorPaint(color) { StrokeThickness = 2f },
        GeometryFill = null,
        GeometryStroke = null,
        GeometrySize = 0,
        Fill = new SolidColorPaint(color.WithAlpha(50)),
        LineSmoothness = 0.6
    };

    private static string FormatCompactNumber(double value)
    {
        double abs = Math.Abs(value);
        if (abs == 0)
        {
            return "0";
        }
        if (abs >= 1000)
        {
            double v = value / 1000;
            return v.ToString("0.#", CultureInfo.InvariantCulture) + "K";
        }
        return value.ToString("0", CultureInfo.InvariantCulture);
    }

    private static string FormatMonthLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return label;

        // Expecting labels like "MMM yyyy" (e.g., "Oct 2025"). Try to parse and reformat as "MMM-yy".
        if (DateTime.TryParseExact(label, "MMM yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out var dt))
        {
            return dt.ToString("MMM-yy", CultureInfo.CurrentCulture);
        }

        // Fallback: if the label ends with a 4-digit year, shorten it to two digits and join with a dash.
        var parts = label.Split(' ',
            StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[^1].Length == 4 && int.TryParse(parts[^1], out _))
        {
            var year2 = parts[^1].Substring(parts[^1].Length - 2);
            var left = string.Join(" ", parts.Take(parts.Length - 1));
            return $"{left}-{year2}";
        }

        // Generic fallback: replace space with dash (e.g., "Oct 2025" -> "Oct-2025").
        return label.Replace(' ', '-');
    }

    private void BuildHero(DashboardSummary s)
    {
        DateTime now = DateTime.Now;
        HeroDateText = now.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture).ToUpper(CultureInfo.CurrentCulture);

        string name = string.IsNullOrWhiteSpace(_session.FullName) ? "there" : _session.FullName!.Split(' ')[0];
        string partOfDay = now.Hour < 12 ? "morning" : now.Hour < 17 ? "afternoon" : "evening";
        GreetingText = $"Good {partOfDay}, {name} \U0001F44B";

        string pnl = s.ProfitLossPercent >= 0
            ? $"up {s.ProfitLossPercent:0.0}%"
            : $"down {Math.Abs(s.ProfitLossPercent):0.0}%";
        int sips = s.UpcomingSips.Count;
        int insights = s.Insights.Count;
        PortfolioInsightText =
            $"Portfolio {pnl} overall · {sips} upcoming SIP{(sips == 1 ? "" : "s")} · " +
            $"{insights} insight{(insights == 1 ? "" : "s")} to review.";
    }

    private static string ActionLabel(InsightTopic topic) => topic switch
    {
        InsightTopic.Spending => "Analyse",
        _ => "Review"
    };

    private static string Money(decimal value) => Richie.Application.Common.CurrencyFormatter.Format(value);
    private static string CompactMoney(decimal value) => Richie.Application.Common.CurrencyFormatter.FormatCompact(value);
}
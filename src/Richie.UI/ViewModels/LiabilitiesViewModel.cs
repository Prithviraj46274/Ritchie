using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Richie.Application.Liabilities;
using Richie.UI.Charts;
using SkiaSharp;

namespace Richie.UI.ViewModels;

public partial class LiabilitiesViewModel : ObservableObject
{
    private readonly ILoanService _loans;

    [ObservableProperty] private ObservableCollection<LoanSummary> _items = [];
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private decimal _totalOutstanding;
    [ObservableProperty] private decimal _totalMonthlyEmi;
    [ObservableProperty] private decimal _totalInterestPayable;
    [ObservableProperty] private int _activeLoanCount;
    [ObservableProperty] private decimal _debtToIncomePercent;
    [ObservableProperty] private int _loanHealthScore;
    [ObservableProperty] private string _loanHealthRating = string.Empty;
    [ObservableProperty] private decimal _totalAssets;
    [ObservableProperty] private decimal _netWorth;
    [ObservableProperty] private ObservableCollection<DebtHealthRow> _debtHealthRows = [];
    [ObservableProperty] private ObservableCollection<string> _recommendations = [];

    [ObservableProperty] private ISeries[] _loanRepaymentSeries = [];
    [ObservableProperty] private Axis[] _loanRepaymentXAxes = [new Axis()];
    [ObservableProperty] private Axis[] _loanRepaymentYAxes = [new Axis()];

    public LiabilitiesViewModel(ILoanService loans)
    {
        _loans = loans;
        Refresh();
    }

    public void Refresh()
    {
        Items = new ObservableCollection<LoanSummary>(_loans.GetLoans());
        IsEmpty = Items.Count == 0;

        LiabilitiesSummary summary = _loans.GetSummary();
        TotalOutstanding = summary.TotalOutstanding;
        TotalMonthlyEmi = summary.TotalMonthlyEmi;
        TotalInterestPayable = summary.TotalInterestPayable;
        ActiveLoanCount = summary.ActiveLoanCount;
        DebtToIncomePercent = summary.DebtToIncomePercent;
        LoanHealthScore = summary.LoanHealthScore;
        LoanHealthRating = summary.LoanHealthRating;

        NetWorthSummary netWorth = _loans.GetNetWorth();
        TotalAssets = netWorth.TotalAssets;
        NetWorth = netWorth.NetWorth;

        DebtHealthReport debtHealth = _loans.GetDebtHealth();
        DebtHealthRows = new ObservableCollection<DebtHealthRow>(debtHealth.Rows);
        Recommendations = new ObservableCollection<string>(debtHealth.Recommendations);

        BuildRepaymentChart();
    }

    private void BuildRepaymentChart()
    {
        var loans = Items.ToList();
        if (loans.Count == 0)
        {
            LoanRepaymentSeries = [];
            LoanRepaymentXAxes = [new Axis { IsVisible = false }];
            LoanRepaymentYAxes = [new Axis { IsVisible = false }];
            return;
        }

        var labels = new string[loans.Count];
        var originalValues = new double?[loans.Count];
        var outstandingValues = new double?[loans.Count];

        for (int i = 0; i < loans.Count; i++)
        {
            LoanSummary summary = loans[i];
            LoanInput? detail = _loans.GetById(summary.Id);
            labels[i] = summary.Provider is { Length: > 0 } p
                ? $"{summary.TypeName} · {p}"
                : summary.TypeName;
            originalValues[i] = detail is not null ? (double)detail.OriginalAmount : null;
            outstandingValues[i] = (double)summary.OutstandingAmount;
        }

        LoanRepaymentSeries =
        [
            new RowSeries<double?>
            {
                Name = "Original Amount",
                Values = originalValues,
                Fill = BrandPalette.Categorical(0),
                MaxBarWidth = 20,
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = p => FormatCompactNumber(p.Coordinate.PrimaryValue)
            },
            new RowSeries<double?>
            {
                Name = "Outstanding",
                Values = outstandingValues,
                Fill = new SolidColorPaint(BrandPalette.Danger),
                MaxBarWidth = 20,
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = p => FormatCompactNumber(p.Coordinate.PrimaryValue)
            }
        ];
        LoanRepaymentXAxes =
        [
            new Axis
            {
                MinLimit = 0,
                Labeler = FormatCompactNumber,
                LabelsPaint = BrandPalette.ChartAxesLabelPaint,
                SeparatorsPaint = BrandPalette.ChartGridLinesPaint
            }
        ];
        LoanRepaymentYAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = BrandPalette.ChartAxesLabelPaint,
                SeparatorsPaint = BrandPalette.ChartGridLinesPaint
            }
        ];
    }

    private static string FormatCompactNumber(double value)
    {
        double abs = Math.Abs(value);
        if (abs == 0) return "0";
        if (abs >= 1_000_000) return (value / 1_000_000).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (abs >= 1_000) return (value / 1_000).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        return value.ToString("0", CultureInfo.InvariantCulture);
    }

    public void Delete(Guid id)
    {
        _loans.Delete(id);
        Refresh();
    }

    public void CloseLoan(Guid id)
    {
        _loans.Close(id);
        Refresh();
    }
}
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Reports;

namespace Richie.UI.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportService _reports;

    public sealed record ReportTypeOption(ReportType Value, string Text);

    public partial class SectionToggle : ObservableObject
    {
        public string Heading { get; init; } = string.Empty;
        [ObservableProperty] private bool _isIncluded = true;
    }

    public IReadOnlyList<ReportTypeOption> ReportTypes { get; } =
    [
        new(ReportType.Assets, "Assets"),
        new(ReportType.Expenses, "Expenses"),
        new(ReportType.Vault, "Password Vault"),
        new(ReportType.Insurance, "Insurance"),
        new(ReportType.FullPortfolio, "Full Portfolio"),
    ];

    [ObservableProperty] private ReportType _selectedType = ReportType.Assets;
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private bool _includeUnmaskedPasswords;
    [ObservableProperty] private ObservableCollection<SectionToggle> _sections = [];
    [ObservableProperty] private string _previewText = "Choose a report type and click Generate preview.";
    [ObservableProperty] private bool _hasReport;

    /// <summary>Whether the chosen report can contain vault passwords (gates the unmask option).</summary>
    public bool IsVaultReport => SelectedType is ReportType.Vault or ReportType.FullPortfolio;

    public ReportsViewModel(IReportService reports) => _reports = reports;

    partial void OnSelectedTypeChanged(ReportType value)
    {
        OnPropertyChanged(nameof(IsVaultReport));
        if (!IsVaultReport)
            IncludeUnmaskedPasswords = false;
    }

    /// <summary>Builds a masked preview and refreshes the section toggles.</summary>
    public void GeneratePreview()
    {
        ReportContent content = _reports.Build(Request(unmasked: false));
        Sections = new ObservableCollection<SectionToggle>(
            content.Sections.Select(s => new SectionToggle { Heading = s.Heading }));
        PreviewText = Render(content);
        HasReport = true;
    }

    /// <summary>Builds the report for export, honouring the section checkboxes and unmask choice.</summary>
    public ReportContent BuildForExport(bool unmasked)
    {
        ReportContent content = _reports.Build(Request(unmasked));
        var included = Sections.Where(s => s.IsIncluded).Select(s => s.Heading).ToHashSet();
        if (included.Count == 0)
            return content;   // no preview / nothing toggled → include everything
        IReadOnlyList<ReportSection> kept = content.Sections.Where(s => included.Contains(s.Heading)).ToList();
        return content with { Sections = kept };
    }

    public string SuggestedFileName(string extension) =>
        $"richie-{SelectedType.ToString().ToLowerInvariant()}-{DateTime.Now:yyyyMMdd}.{extension}";

    private ReportRequest Request(bool unmasked) => new(SelectedType, FromDate, ToDate, unmasked);

    private static string Render(ReportContent content)
    {
        var sb = new StringBuilder();
        sb.AppendLine(content.Title);
        sb.AppendLine($"Period: {content.PeriodLabel}");
        sb.AppendLine();
        foreach (ReportSection section in content.Sections)
        {
            sb.AppendLine($"■ {section.Heading}");
            foreach (string line in section.Lines)
                sb.AppendLine($"    {line}");
            if (section.Table is { } table)
            {
                sb.AppendLine($"    {string.Join("   |   ", table.Columns)}");
                foreach (IReadOnlyList<string> row in table.Rows)
                    sb.AppendLine($"    {string.Join("   |   ", row)}");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

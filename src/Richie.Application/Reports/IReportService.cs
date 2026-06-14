namespace Richie.Application.Reports;

public enum ReportType { Assets, Expenses, Vault, FullPortfolio, Insurance }

/// <summary>A simple table for a report section.</summary>
public sealed record ReportTable(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>One section of a report — free text lines and/or a table.</summary>
public sealed record ReportSection(string Heading, IReadOnlyList<string> Lines, ReportTable? Table = null);

/// <summary>A fully-built report, ready to render to PDF/PPTX.</summary>
public sealed record ReportContent(
    string Title, DateTime GeneratedUtc, string PeriodLabel, IReadOnlyList<ReportSection> Sections);

/// <summary>What to build. <paramref name="IncludeUnmaskedPasswords"/> reveals vault passwords and
/// must only be set after a master-password re-auth + explicit user confirmation (PRD §12).</summary>
public sealed record ReportRequest(ReportType Type, DateTime? From, DateTime? To, bool IncludeUnmaskedPasswords);

/// <summary>Builds report content from the user's real data across modules.</summary>
public interface IReportService
{
    ReportContent Build(ReportRequest request);
}

/// <summary>Renders a <see cref="ReportContent"/> to a file format.</summary>
public interface IReportExporter
{
    byte[] ToPdf(ReportContent content);
    byte[] ToPptx(ReportContent content);
}

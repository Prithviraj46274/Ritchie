namespace Richie.Application.Reports;

public enum ReportType { Assets, Expenses, Vault, FullPortfolio, Insurance }

// ──────────────────────────────────────────────────────────────────────────────
// Existing core types (unchanged)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>A simple table for a report section.</summary>
/// <param name="SignedColumns">Column indices whose cells hold a signed money/percent value and should be
/// coloured green when positive / red when negative (PDF + Excel only; CSV/PPTX have no colour).</param>
/// <param name="LinkColumns">Column indices whose cells should render as a hyperlink to the row's
/// <see cref="RowLinks"/> URL (PDF + Excel only).</param>
/// <param name="RowLinks">One URL per row (null where absent) used by <see cref="LinkColumns"/>.</param>
public sealed record ReportTable(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<int>? SignedColumns = null,
    IReadOnlyList<int>? LinkColumns = null,
    IReadOnlyList<string?>? RowLinks = null);

public enum ReportChartKind { Pie, Column, Donut }

/// <summary>One labelled data point for a report chart.</summary>
public sealed record ReportChartPoint(string Label, double Value);

/// <summary>A chart specification for a section — pure data; the exporter renders it to an image.</summary>
public sealed record ReportChart(
    ReportChartKind Kind,
    IReadOnlyList<ReportChartPoint> Points,
    bool IsLarge = false,
    string? LargestLabel = null);

// ──────────────────────────────────────────────────────────────────────────────
// New: Premium report enrichment types
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Visual sentiment for a KPI card value.</summary>
public enum KpiSentiment { Neutral, Positive, Negative, Warning }

/// <summary>Visual severity for an insight line.</summary>
public enum InsightLevel { Info, Positive, Warning, Alert }

/// <summary>
/// A single KPI metric displayed as a premium card in the Executive Summary and other
/// summary sections.  <paramref name="SubLabel"/> is shown in smaller text below the value
/// (e.g. "+12.4% this year").
/// </summary>
public sealed record ReportKpiCard(
    string Label,
    string Value,
    string? SubLabel = null,
    KpiSentiment Sentiment = KpiSentiment.Neutral);

/// <summary>
/// A rich per-asset card rendered in the Asset Detail Cards section.
/// <paramref name="ThumbnailPng"/> is an in-memory PNG (≈80×80 px); null when the asset
/// has no uploaded images.
/// </summary>
public sealed record ReportAssetCard(
    string Name,
    string TypeName,
    string PurchaseDate,
    string Invested,
    string CurrentValue,
    string GainLoss,
    string ReturnPercent,
    string? Notes,
    int DocumentCount,
    int ImageCount,
    byte[]? ThumbnailPng,
    KpiSentiment ReturnSentiment);

/// <summary>A single insight / observation line with colour-coded severity.</summary>
public sealed record ReportInsight(string Text, InsightLevel Level = InsightLevel.Info);

// ──────────────────────────────────────────────────────────────────────────────
// Existing ReportSection — extended with optional premium fields
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// One section of a report.  All new fields are optional so existing XLSX/CSV/PPTX
/// exporters continue to work without modification.
/// </summary>
public sealed record ReportSection(
    string Heading,
    IReadOnlyList<string> Lines,
    ReportTable? Table = null,
    ReportChart? Chart = null,
    // ── Premium enrichment (PDF only; ignored by XLSX/CSV/PPTX) ──────────────
    IReadOnlyList<ReportKpiCard>? KpiCards = null,
    IReadOnlyList<ReportAssetCard>? AssetCards = null,
    IReadOnlyList<ReportInsight>? Insights = null);

/// <summary>A fully-built report, ready to render to PDF/PPTX.</summary>
public sealed record ReportContent(
    string Title, DateTime GeneratedUtc, string PeriodLabel, IReadOnlyList<ReportSection> Sections);

/// <summary>What to build.  <paramref name="IncludeUnmaskedPasswords"/> reveals vault passwords and
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

    /// <summary>Excel workbook — one worksheet per section (charts omitted; data shown as tables).</summary>
    byte[] ToXlsx(ReportContent content);

    /// <summary>Flat CSV — sections serialised sequentially (charts omitted; data shown as tables).</summary>
    byte[] ToCsv(ReportContent content);
}

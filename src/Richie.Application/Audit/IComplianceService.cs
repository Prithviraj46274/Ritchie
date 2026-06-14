namespace Richie.Application.Audit;

public enum ComplianceStatus { Green, Amber, Red }

/// <summary>One compliance area with a traffic-light status and explanation (PRD §9.2).</summary>
public sealed record ComplianceArea(string Name, ComplianceStatus Status, string Detail);

/// <summary>A guaranteed investment plan (GIP) tracked for maturity + guaranteed return (PRD §9.2).</summary>
public sealed record GipStatusRow(
    string Name, decimal? GuaranteedReturnPercent, DateTime? MaturityDate, string Status);

/// <summary>Compliance dashboard (PRD §9.2): per-area green/amber/red, overall compliant flag,
/// and guaranteed-investment tracking.</summary>
public sealed record ComplianceReport(
    bool IsCompliant,
    string OverallStatus,
    IReadOnlyList<ComplianceArea> Areas,
    IReadOnlyList<GipStatusRow> Gips);

public interface IComplianceService
{
    ComplianceReport GetReport();
}

using Richie.Application.Audit;
using Richie.Application.Authentication;
using Richie.Domain.Assets;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Audit;

/// <summary>
/// Compliance dashboard (PRD §9.2). Reuses the Financial Health Audit for benchmark/coverage/
/// diversification status and adds guaranteed-investment (GIP) maturity tracking read from assets.
/// </summary>
public sealed class ComplianceService : IComplianceService
{
    private const int GipMaturityWarnDays = 90;

    private readonly IHealthAuditService _audit;
    private readonly IUserSession _session;
    private readonly IAppDbContextFactory _factory;
    private readonly Application.Abstractions.IClock _clock;

    public ComplianceService(
        IHealthAuditService audit, IUserSession session, IAppDbContextFactory factory,
        Application.Abstractions.IClock clock)
    {
        _audit = audit;
        _session = session;
        _factory = factory;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public ComplianceReport GetReport()
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        HealthAuditReport audit = _audit.GetReport();

        var areas = new List<ComplianceArea>();

        // 1. Asset allocation vs benchmark.
        if (!audit.HasAssets)
        {
            areas.Add(new ComplianceArea("Asset allocation", ComplianceStatus.Red,
                "No assets recorded — add holdings to assess allocation."));
        }
        else
        {
            int offTarget = audit.Benchmark.Count(b => b.Status != BenchmarkStatus.OnTarget);
            ComplianceStatus allocStatus = offTarget == 0 ? ComplianceStatus.Green
                : offTarget <= 2 ? ComplianceStatus.Amber : ComplianceStatus.Red;
            areas.Add(new ComplianceArea("Asset allocation", allocStatus,
                offTarget == 0 ? "Allocation is within the recommended ranges for your age group."
                               : $"{offTarget} asset class(es) outside the recommended range."));
        }

        // 2. Diversification.
        ComplianceStatus divStatus = audit.DistinctClassCount >= 3 ? ComplianceStatus.Green
            : audit.DistinctClassCount == 2 ? ComplianceStatus.Amber : ComplianceStatus.Red;
        areas.Add(new ComplianceArea("Diversification", divStatus,
            $"{audit.DistinctClassCount} of 4 broad asset classes represented."));

        // 3. Protection coverage.
        ComplianceStatus coverageStatus = audit.CoverageGaps.Count == 0 ? ComplianceStatus.Green
            : audit.CoverageGaps.Count == 1 ? ComplianceStatus.Amber : ComplianceStatus.Red;
        areas.Add(new ComplianceArea("Protection coverage", coverageStatus,
            audit.CoverageGaps.Count == 0 ? "Health and term-life cover are in place."
                                          : string.Join(" ", audit.CoverageGaps)));

        // 4. Guaranteed investments (GIP) tracking.
        List<GipStatusRow> gips = ReadGips(userId, now, out ComplianceStatus gipStatus, out string gipDetail);
        areas.Add(new ComplianceArea("Guaranteed investments", gipStatus, gipDetail));

        bool compliant = areas.All(a => a.Status == ComplianceStatus.Green);
        string overall = compliant ? "Compliant"
            : areas.Any(a => a.Status == ComplianceStatus.Red) ? "Non-compliant — needs attention"
            : "Mostly compliant — minor flags";

        return new ComplianceReport(compliant, overall, areas, gips);
    }

    private List<GipStatusRow> ReadGips(Guid userId, DateTime now, out ComplianceStatus status, out string detail)
    {
        using RichieDbContext db = _factory.Create();
        List<Asset> gipAssets = db.Assets
            .Where(a => a.UserId == userId && a.Type == AssetType.GuaranteedInvestmentPlan)
            .OrderBy(a => a.MaturityDate)
            .ToList();

        if (gipAssets.Count == 0)
        {
            status = ComplianceStatus.Green;
            detail = "No guaranteed investment plans tracked.";
            return [];
        }

        var rows = new List<GipStatusRow>();
        bool anyMatured = false, anyNear = false;
        foreach (Asset a in gipAssets)
        {
            string rowStatus;
            if (a.MaturityDate is not { } maturity)
            {
                rowStatus = "No maturity date set";
            }
            else
            {
                int days = (int)Math.Ceiling((maturity - now).TotalDays);
                if (days < 0) { rowStatus = "Matured"; anyMatured = true; }
                else if (days <= GipMaturityWarnDays) { rowStatus = $"Matures in {days} day(s)"; anyNear = true; }
                else rowStatus = $"Matures {maturity:d}";
            }
            rows.Add(new GipStatusRow(a.Name, a.GuaranteedReturnPercent, a.MaturityDate, rowStatus));
        }

        status = anyMatured ? ComplianceStatus.Red : anyNear ? ComplianceStatus.Amber : ComplianceStatus.Green;
        detail = anyMatured ? "One or more plans have matured — review reinvestment."
            : anyNear ? "A plan is approaching maturity."
            : $"{rows.Count} plan(s) tracked, none maturing soon.";
        return rows;
    }
}

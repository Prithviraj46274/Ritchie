using System.Globalization;
using Richie.Application.Abstractions;
using Richie.Application.Assets;
using Richie.Application.Expenses;
using Richie.Application.Income;
using Richie.Application.Insurance;
using Richie.Application.Reports;
using Richie.Application.Vault;
using Richie.Domain.Assets;
using SkiaSharp;

namespace Richie.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private const string Masked = "••••••••";

    private readonly IAssetService _assets;
    private readonly IValuationService _valuation;
    private readonly IGoalService _goals;
    private readonly IExpenseService _expenses;
    private readonly IExpenseAnalyticsService _expenseAnalytics;
    private readonly IIncomeService _income;
    private readonly IVaultService _vault;
    private readonly IInsuranceService _insurance;
    private readonly IAssetDocumentService _docs;
    private readonly IClock _clock;

    public ReportService(
        IAssetService assets, IValuationService valuation, IGoalService goals,
        IExpenseService expenses, IExpenseAnalyticsService expenseAnalytics, IIncomeService income,
        IVaultService vault, IInsuranceService insurance, IAssetDocumentService docs, IClock clock)
    {
        _assets = assets;
        _valuation = valuation;
        _goals = goals;
        _expenses = expenses;
        _expenseAnalytics = expenseAnalytics;
        _income = income;
        _vault = vault;
        _insurance = insurance;
        _docs = docs;
        _clock = clock;
    }

    public ReportContent Build(ReportRequest request)
    {
        var sections = new List<ReportSection>();
        switch (request.Type)
        {
            case ReportType.Assets:
                sections.AddRange(AssetSections());
                break;
            case ReportType.Expenses:
                sections.AddRange(ExpenseSections(request));
                break;
            case ReportType.Vault:
                sections.Add(VaultSection(request.IncludeUnmaskedPasswords));
                break;
            case ReportType.Insurance:
                sections.AddRange(InsuranceSections());
                break;
            case ReportType.FullPortfolio:
                // ── Executive Summary first ──────────────────────────────────
                sections.Add(ExecutiveSummarySection(request));
                // ── Existing asset / expense / insurance / vault sections ────
                sections.AddRange(AssetSections());
                sections.AddRange(ExpenseSections(request));
                sections.AddRange(InsuranceSections());
                sections.Add(VaultSection(request.IncludeUnmaskedPasswords));
                // ── New analytical / intelligence sections ───────────────────
                sections.Add(PortfolioHealthSection());
                sections.Add(ConcentrationAnalysisSection());
                sections.Add(TopPerformersSection());
                sections.Add(AssetDetailCardsSection());
                sections.AddRange(ExpenseInsightsSections(request));
                sections.Add(IncomeInsightsSection(request));
                sections.Add(NetWorthBreakdownSection(request));
                sections.Add(AssetTimelineSection());
                sections.Add(DocumentSummarySection());
                break;
        }

        string period = request is { From: { } f, To: { } t }
            ? $"{f:d} – {t:d}"
            : "All data";
        return new ReportContent(Title(request.Type), _clock.UtcNow, period, sections);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EXISTING SECTIONS (unchanged)
    // ══════════════════════════════════════════════════════════════════════════

    private IEnumerable<ReportSection> AssetSections()
    {
        PortfolioSummary p = _assets.GetPortfolioSummary();
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();

        // Totals as a one-row table so the Profit/Loss amount and Return % can be colour-coded
        // (the signed columns) just like the per-holding P&L below.
        yield return new ReportSection("Portfolio summary", [],
            new ReportTable(
                ["Total invested", "Total current value", "Profit / Loss", "Return %"],
                [[
                    Money(p.TotalInvested), Money(p.TotalCurrentValue), Money(p.TotalProfitLoss),
                    p.TotalProfitLossPercent.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "%"
                ]],
                SignedColumns: [2, 3]));

        yield return new ReportSection("Holdings", [],
            new ReportTable(
                ["Name", "Type", "Invested", "Current", "P&L"],
                assets.Select(a => (IReadOnlyList<string>)
                [
                    a.Name, a.TypeName, Money(a.InvestedAmount), Money(a.CurrentValue), Money(a.ProfitLoss)
                ]).ToList(),
                SignedColumns: [4]));

        yield return new ReportSection("Allocation by type", [],
            new ReportTable(
                ["Type", "Value", "Share"],
                p.Allocation.Select(s => (IReadOnlyList<string>)
                    [s.TypeName, Money(s.Value), $"{s.Percent:0.#}%"]).ToList()),
            new ReportChart(ReportChartKind.Donut,
                p.Allocation.Select(s => new ReportChartPoint(s.TypeName, (double)s.Value)).ToList(),
                IsLarge: true,
                LargestLabel: p.Allocation.OrderByDescending(s => s.Percent).FirstOrDefault()?.TypeName));

        IReadOnlyList<GoalProgress> goals = _goals.GetGoals();
        if (goals.Count > 0)
        {
            yield return new ReportSection("Goal progress", [],
                new ReportTable(
                    ["Goal", "Progress", "Current", "Target"],
                    goals.Select(g => (IReadOnlyList<string>)
                    [
                        g.Name, $"{g.PercentComplete:0.#}%", Money(g.CurrentValue), Money(g.TargetAmount)
                    ]).ToList()));
        }
    }

    private IEnumerable<ReportSection> ExpenseSections(ReportRequest request)
    {
        var filter = new ExpenseFilter(From: request.From, To: request.To);
        IReadOnlyList<ExpenseSummary> expenses = _expenses.GetExpenses(filter);

        var byCategory = expenses
            .GroupBy(e => e.CategoryName)
            .Select(g => (Category: g.Key, Amount: g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        yield return new ReportSection("Expenses by category",
            [$"Total: {Money(expenses.Sum(e => e.Amount))} across {expenses.Count} entries"],
            new ReportTable(
                ["Category", "Amount"],
                byCategory.Select(c => (IReadOnlyList<string>)[c.Category, Money(c.Amount)]).ToList()),
            new ReportChart(ReportChartKind.Pie,
                byCategory.Select(c => new ReportChartPoint(c.Category, (double)c.Amount)).ToList(),
                IsLarge: true));

        IReadOnlyList<PeriodDatum> monthly = _expenseAnalytics.GetMonthlyTotals(12);
        yield return new ReportSection("Monthly trend (last 12 months)", [],
            new ReportTable(
                ["Month", "Spend"],
                monthly.Select(m => (IReadOnlyList<string>)[m.Label, Money(m.Amount)]).ToList()),
            new ReportChart(ReportChartKind.Column,
                monthly.Select(m => new ReportChartPoint(m.Label, (double)m.Amount)).ToList(),
                IsLarge: true));

        IReadOnlyList<IncomeSummary> income = _income.GetRecent(500);
        decimal totalIncome = income.Sum(i => i.Amount);
        var bySource = income
            .GroupBy(i => i.Source)
            .Select(g => (Source: g.Key, Amount: g.Sum(i => i.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();
        yield return new ReportSection("Income",
            [$"Total income: {Money(totalIncome)} across {income.Count} entries",
             $"Net (income − expenses): {Money(totalIncome - expenses.Sum(e => e.Amount))}"],
            new ReportTable(
                ["Source", "Amount"],
                bySource.Select(s => (IReadOnlyList<string>)[s.Source, Money(s.Amount)]).ToList()));
    }

    private ReportSection VaultSection(bool unmasked)
    {
        IReadOnlyList<VaultEntrySummary> entries = _vault.GetEntries();
        var rows = entries.Select(e => (IReadOnlyList<string>)
        [
            e.AccountName, e.Category ?? "", e.LoginId ?? "",
            unmasked ? _vault.RevealPassword(e.Id) ?? "" : Masked,
            e.Url ?? ""
        ]).ToList();
        var links = entries.Select(e => string.IsNullOrWhiteSpace(e.Url) ? null : e.Url).ToList();

        return new ReportSection("Vault accounts",
            unmasked
                ? ["Passwords are shown UNMASKED — handle this export securely."]
                : ["Passwords are masked."],
            new ReportTable(
                ["Account", "Category", "User ID", "Password", "Website"], rows,
                LinkColumns: [0, 4], RowLinks: links));
    }

    private IEnumerable<ReportSection> InsuranceSections()
    {
        IReadOnlyList<InsurancePolicySummary> policies = _insurance.GetPolicies();
        yield return new ReportSection("Insurance policies",
            [$"{policies.Count} policy(ies). Insurance is tracked separately and is not part of asset allocation."],
            new ReportTable(
                ["Type", "Policy", "Provider", "Coverage", "Premium/yr", "Renewal"],
                policies.Select(p => (IReadOnlyList<string>)
                [
                    p.TypeName, p.PolicyName, p.Provider ?? "", Money(p.CoverageAmount),
                    Money(p.AnnualPremium), p.RenewalDate.ToString("d", CultureInfo.CurrentCulture)
                ]).ToList()));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // NEW SECTIONS — Full Portfolio only
    // ══════════════════════════════════════════════════════════════════════════

    private ReportSection ExecutiveSummarySection(ReportRequest request)
    {
        PortfolioSummary p = _assets.GetPortfolioSummary();
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();
        var filter = new ExpenseFilter(From: request.From, To: request.To);
        IReadOnlyList<ExpenseSummary> expenses = _expenses.GetExpenses(filter);
        IReadOnlyList<IncomeSummary> income = _income.GetRecent(500);

        decimal totalExpenses = expenses.Sum(e => e.Amount);
        decimal totalIncome = income.Sum(i => i.Amount);
        decimal netCashFlow = totalIncome - totalExpenses;
        decimal monthlyExpense = _expenseAnalytics.GetMonthlyTotals(1).FirstOrDefault()?.Amount ?? 0m;
        decimal monthlyIncome = _income.GetMonthlyTotal();

        int categoryCount = assets.Select(a => a.TypeName).Distinct().Count();

        var kpiCards = new List<ReportKpiCard>
        {
            new("Total Net Worth", Money(p.TotalCurrentValue), "Asset portfolio value",
                p.TotalCurrentValue > 0 ? KpiSentiment.Positive : KpiSentiment.Neutral),
            new("Total Invested", Money(p.TotalInvested), "Capital deployed"),
            new("Total Current Value", Money(p.TotalCurrentValue), "Market value today",
                p.TotalCurrentValue >= p.TotalInvested ? KpiSentiment.Positive : KpiSentiment.Negative),
            new("Total Profit / Loss", Money(p.TotalProfitLoss),
                p.TotalProfitLossPercent.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "% overall",
                p.TotalProfitLoss > 0 ? KpiSentiment.Positive : p.TotalProfitLoss < 0 ? KpiSentiment.Negative : KpiSentiment.Neutral),
            new("Return %",
                p.TotalProfitLossPercent.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "%",
                "On invested capital",
                p.TotalProfitLossPercent > 0 ? KpiSentiment.Positive : p.TotalProfitLossPercent < 0 ? KpiSentiment.Negative : KpiSentiment.Neutral),
            new("Number of Assets", assets.Count.ToString(), $"{categoryCount} categories"),
            new("Asset Categories", categoryCount.ToString(), "Distinct asset types"),
            new("Monthly Expenses", Money(monthlyExpense), "Current month spend",
                KpiSentiment.Warning),
            new("Monthly Income", Money(monthlyIncome), "Current month income",
                KpiSentiment.Positive),
            new("Net Cash Flow", Money(netCashFlow), "Income − Expenses (all time)",
                netCashFlow >= 0 ? KpiSentiment.Positive : KpiSentiment.Negative),
        };

        return new ReportSection("Executive Summary", [], KpiCards: kpiCards);
    }

    private ReportSection PortfolioHealthSection()
    {
        PortfolioSummary p = _assets.GetPortfolioSummary();
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();

        // ── Diversification Score (0–100) ─────────────────────────────────────
        // Based on Herfindahl-Hirschman Index (HHI): lower HHI = more diversified.
        // HHI = Σ(share²); max diversification = 0 (impossible), min = 1 (all in one).
        int categoryCount = p.Allocation.Count;
        double hhi = p.Allocation.Sum(s => Math.Pow((double)s.Percent / 100.0, 2));
        // Normalise: 0 = perfectly concentrated (hhi=1), 100 = perfectly spread.
        double diversificationScore = categoryCount <= 1 ? 0 : Math.Round((1.0 - hhi) / (1.0 - 1.0 / categoryCount) * 100, 0);
        diversificationScore = Math.Max(0, Math.Min(100, diversificationScore));
        string divLabel = ScoreLabel((int)diversificationScore);

        // ── Concentration Score (inverse of diversification) ──────────────────
        double largestPct = p.Allocation.Count > 0 ? (double)p.Allocation.Max(s => s.Percent) : 100;
        int concentrationScore = (int)Math.Round(largestPct);
        string concLabel = largestPct >= 75 ? "High" : largestPct >= 50 ? "Moderate" : "Low";

        // ── Liquidity Score ───────────────────────────────────────────────────
        // Liquid assets: MutualFund, Equity, DigitalGold, SovereignGoldBond
        // Illiquid: RealEstate, GoldJewellery, GuaranteedInvestmentPlan
        decimal totalValue = p.TotalCurrentValue > 0 ? p.TotalCurrentValue : 1m;
        decimal liquidValue = assets
            .Where(a => a.Type is AssetType.MutualFund or AssetType.Equity or AssetType.DigitalGold or AssetType.SovereignGoldBond)
            .Sum(a => a.CurrentValue);
        int liquidityScore = (int)Math.Round((double)(liquidValue / totalValue) * 100);
        string liqLabel = ScoreLabel(liquidityScore);

        // ── Risk Rating ───────────────────────────────────────────────────────
        // High equity/real-estate concentration = higher risk
        decimal highRiskValue = assets
            .Where(a => a.Type is AssetType.Equity or AssetType.RealEstate)
            .Sum(a => a.CurrentValue);
        int riskScore = (int)Math.Round((double)(highRiskValue / totalValue) * 100);
        string riskLabel = riskScore >= 75 ? "High" : riskScore >= 50 ? "Moderate-High" : riskScore >= 25 ? "Moderate" : "Low";

        var insights = new List<ReportInsight>
        {
            new($"Diversification Score: {(int)diversificationScore}/100 — {divLabel}",
                diversificationScore >= 60 ? InsightLevel.Positive : diversificationScore >= 35 ? InsightLevel.Warning : InsightLevel.Alert),
            new($"Concentration Score: {concLabel} ({largestPct:0.#}% in single category)",
                largestPct < 50 ? InsightLevel.Positive : largestPct < 75 ? InsightLevel.Warning : InsightLevel.Alert),
            new($"Liquidity Score: {liquidityScore}/100 — {liqLabel}",
                liquidityScore >= 50 ? InsightLevel.Positive : InsightLevel.Warning),
            new($"Portfolio Risk Rating: {riskLabel} ({riskScore}% in high-volatility assets)",
                riskScore < 50 ? InsightLevel.Info : riskScore < 75 ? InsightLevel.Warning : InsightLevel.Alert),
        };

        var kpiCards = new List<ReportKpiCard>
        {
            new("Diversification Score", $"{(int)diversificationScore}/100", divLabel,
                diversificationScore >= 60 ? KpiSentiment.Positive : diversificationScore >= 35 ? KpiSentiment.Warning : KpiSentiment.Negative),
            new("Concentration Risk", concLabel, $"{largestPct:0.#}% in top category",
                largestPct < 50 ? KpiSentiment.Positive : largestPct < 75 ? KpiSentiment.Warning : KpiSentiment.Negative),
            new("Liquidity Score", $"{liquidityScore}/100", liqLabel,
                liquidityScore >= 50 ? KpiSentiment.Positive : KpiSentiment.Warning),
            new("Risk Rating", riskLabel, $"{riskScore}% high-volatility",
                riskScore < 50 ? KpiSentiment.Neutral : riskScore < 75 ? KpiSentiment.Warning : KpiSentiment.Negative),
        };

        return new ReportSection("Portfolio Health Analysis",
            [$"Analysis based on {assets.Count} assets across {categoryCount} categories."],
            KpiCards: kpiCards,
            Insights: insights);
    }

    private ReportSection ConcentrationAnalysisSection()
    {
        PortfolioSummary p = _assets.GetPortfolioSummary();
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();

        var largestCategory = p.Allocation.OrderByDescending(s => s.Percent).FirstOrDefault();
        var largestAsset = assets.OrderByDescending(a => a.CurrentValue).FirstOrDefault();

        var lines = new List<string>();
        var insights = new List<ReportInsight>();

        if (largestCategory != null)
        {
            lines.Add($"Largest category: {largestCategory.TypeName} ({largestCategory.Percent:0.#}% of portfolio)");
            string concentrationText = largestCategory.Percent >= 75
                ? $"⚠ Portfolio is highly concentrated in {largestCategory.TypeName} ({largestCategory.Percent:0.#}%). Consider diversifying."
                : largestCategory.Percent >= 50
                    ? $"Portfolio has moderate concentration in {largestCategory.TypeName} ({largestCategory.Percent:0.#}%)."
                    : $"Portfolio is well diversified. Largest category ({largestCategory.TypeName}) holds {largestCategory.Percent:0.#}%.";

            insights.Add(new(concentrationText,
                largestCategory.Percent >= 75 ? InsightLevel.Alert :
                largestCategory.Percent >= 50 ? InsightLevel.Warning : InsightLevel.Positive));
        }

        if (largestAsset != null)
        {
            decimal totalValue = p.TotalCurrentValue > 0 ? p.TotalCurrentValue : 1m;
            decimal assetPct = largestAsset.CurrentValue / totalValue * 100m;
            lines.Add($"Largest single holding: {largestAsset.Name} ({assetPct:0.#}% of portfolio)");
            insights.Add(new($"Largest holding: {largestAsset.Name} — {Money(largestAsset.CurrentValue)} ({assetPct:0.#}%)",
                assetPct >= 50 ? InsightLevel.Warning : InsightLevel.Info));
        }

        if (p.Allocation.Count <= 1)
            insights.Add(new("All assets are in a single category. Diversification is strongly recommended.", InsightLevel.Alert));
        else if (p.Allocation.Count >= 4)
            insights.Add(new($"Portfolio spans {p.Allocation.Count} asset categories — good breadth of diversification.", InsightLevel.Positive));

        return new ReportSection("Portfolio Concentration Analysis", lines,
            new ReportTable(
                ["Category", "Value", "% of Portfolio"],
                p.Allocation.OrderByDescending(s => s.Percent).Select(s =>
                    (IReadOnlyList<string>)[s.TypeName, Money(s.Value), $"{s.Percent:0.#}%"]).ToList()),
            Insights: insights);
    }

    private ReportSection TopPerformersSection()
    {
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();
        var ranked = assets.OrderByDescending(a => a.ProfitLossPercent).ToList();
        var top5 = ranked.Take(5).ToList();

        var insights = new List<ReportInsight>();
        if (top5.Count > 0)
        {
            var best = top5[0];
            insights.Add(new(
                $"Top performer: {best.Name} ({best.TypeName}) with {best.ProfitLossPercent:+0.0;-0.0;0.0}% return.",
                best.ProfitLossPercent > 0 ? InsightLevel.Positive : InsightLevel.Info));
        }

        var worst = assets.OrderBy(a => a.ProfitLossPercent).FirstOrDefault();
        if (worst != null && worst.ProfitLossPercent < 0)
            insights.Add(new($"Weakest performer: {worst.Name} with {worst.ProfitLossPercent:+0.0;-0.0;0.0}% return.", InsightLevel.Warning));

        return new ReportSection("Top 5 Performers", [],
            new ReportTable(
                ["Rank", "Asset", "Type", "Invested", "Current Value", "Profit / Loss", "Return %"],
                top5.Select((a, i) => (IReadOnlyList<string>)[
                    $"#{i + 1}", a.Name, a.TypeName, Money(a.InvestedAmount),
                    Money(a.CurrentValue), Money(a.ProfitLoss),
                    a.ProfitLossPercent.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "%"
                ]).ToList(),
                SignedColumns: [5, 6]),
            Insights: insights);
    }

    private ReportSection AssetDetailCardsSection()
    {
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();
        var cards = new List<ReportAssetCard>();

        foreach (AssetSummary asset in assets)
        {
            var docs = _docs.GetForAsset(asset.Id);
            int imageCount = docs.Count(d => d.Kind == Domain.Assets.DocumentKind.Image);
            int docCount = docs.Count;

            byte[]? thumbnail = null;
            if (asset.HasImages && imageCount > 0)
            {
                var firstImage = docs.FirstOrDefault(d => d.Kind == Domain.Assets.DocumentKind.Image);
                if (firstImage != null)
                {
                    try
                    {
                        byte[] raw = _docs.OpenContent(firstImage.Id);
                        thumbnail = ResizeToThumbnail(raw, 80);
                    }
                    catch
                    {
                        thumbnail = null; // skip if decode fails
                    }
                }
            }

            KpiSentiment sentiment = asset.ProfitLoss > 0 ? KpiSentiment.Positive
                : asset.ProfitLoss < 0 ? KpiSentiment.Negative
                : KpiSentiment.Neutral;

            cards.Add(new ReportAssetCard(
                Name: asset.Name,
                TypeName: asset.TypeName,
                PurchaseDate: "—",          // AssetSummary doesn't carry date; Asset entity does
                Invested: Money(asset.InvestedAmount),
                CurrentValue: Money(asset.CurrentValue),
                GainLoss: Money(asset.ProfitLoss),
                ReturnPercent: asset.ProfitLossPercent.ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture) + "%",
                Notes: null,
                DocumentCount: docCount,
                ImageCount: imageCount,
                ThumbnailPng: thumbnail,
                ReturnSentiment: sentiment));
        }

        return new ReportSection("Asset Detail Cards", [], AssetCards: cards);
    }

    private IEnumerable<ReportSection> ExpenseInsightsSections(ReportRequest request)
    {
        var filter = new ExpenseFilter(From: request.From, To: request.To);
        IReadOnlyList<ExpenseSummary> expenses = _expenses.GetExpenses(filter);
        if (expenses.Count == 0) yield break;

        var byCategory = expenses
            .GroupBy(e => e.CategoryName)
            .Select(g => (Category: g.Key, Amount: g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        decimal total = expenses.Sum(e => e.Amount);
        decimal avg = _expenseAnalytics.GetMonthlyTotals(12).Where(m => m.Amount > 0).Select(m => m.Amount)
            .DefaultIfEmpty(0m).Average();
        var largest = byCategory.FirstOrDefault();
        decimal largestPct = total > 0 && largest.Amount > 0 ? largest.Amount / total * 100m : 0m;

        var insights = new List<ReportInsight>();
        if (largest.Category != null)
        {
            insights.Add(new(
                $"{largest.Category} represents {largestPct:0.#}% of all spending — the largest expense category.",
                largestPct >= 40 ? InsightLevel.Warning : InsightLevel.Info));
        }
        insights.Add(new($"Average monthly expenditure: {Money(avg)}", InsightLevel.Info));
        if (byCategory.Count == 1)
            insights.Add(new("All expenses fall in a single category — consider tracking across more categories.", InsightLevel.Warning));
        else if (byCategory.Count >= 5)
            insights.Add(new($"Expenses span {byCategory.Count} categories — good expense visibility.", InsightLevel.Positive));

        yield return new ReportSection("Expense Insights",
            [$"Largest category: {largest.Category ?? "N/A"} ({largestPct:0.#}%)",
             $"Average monthly spend: {Money(avg)}",
             $"Expense concentration in top category: {largestPct:0.#}%"],
            Insights: insights);
    }

    private ReportSection IncomeInsightsSection(ReportRequest request)
    {
        IReadOnlyList<IncomeSummary> income = _income.GetRecent(500);
        if (income.Count == 0)
            return new ReportSection("Income Insights", ["No income records found."]);

        var bySource = income
            .GroupBy(i => i.Source)
            .Select(g => (Source: g.Key, Amount: g.Sum(i => i.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        decimal totalIncome = income.Sum(i => i.Amount);
        var largestSource = bySource.FirstOrDefault();
        decimal largestPct = totalIncome > 0 && largestSource.Amount > 0
            ? largestSource.Amount / totalIncome * 100m : 0m;

        // Passive income heuristic: source name contains passive keywords
        var passiveKeywords = new[] { "dividend", "rent", "interest", "royalty", "passive", "rental" };
        decimal passiveAmount = income
            .Where(i => passiveKeywords.Any(k => i.Source.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Sum(i => i.Amount);
        decimal activeAmount = totalIncome - passiveAmount;
        decimal passivePct = totalIncome > 0 ? passiveAmount / totalIncome * 100m : 0m;
        decimal activePct = 100m - passivePct;

        int diversificationScore = bySource.Count switch
        {
            1 => 20,
            2 => 40,
            3 => 60,
            4 or 5 => 80,
            _ => 100
        };

        var kpiCards = new List<ReportKpiCard>
        {
            new("Largest Income Source", largestSource.Source ?? "N/A",
                $"{largestPct:0.#}% of total income",
                largestPct < 60 ? KpiSentiment.Positive : KpiSentiment.Warning),
            new("Income Diversification", $"{diversificationScore}/100",
                $"{bySource.Count} source(s)",
                diversificationScore >= 60 ? KpiSentiment.Positive : KpiSentiment.Warning),
            new("Passive Income", Money(passiveAmount), $"{passivePct:0.#}% of total",
                passivePct >= 30 ? KpiSentiment.Positive : KpiSentiment.Neutral),
            new("Active Income", Money(activeAmount), $"{activePct:0.#}% of total"),
        };

        var insights = new List<ReportInsight>
        {
            new($"Primary income source: {largestSource.Source ?? "N/A"} ({largestPct:0.#}% of total)",
                largestPct < 70 ? InsightLevel.Info : InsightLevel.Warning),
            new($"Income diversification score: {diversificationScore}/100 across {bySource.Count} source(s)",
                diversificationScore >= 60 ? InsightLevel.Positive : InsightLevel.Warning),
            new($"Passive income: {passivePct:0.#}% | Active income: {activePct:0.#}% (heuristic estimate)",
                passivePct >= 30 ? InsightLevel.Positive : InsightLevel.Info),
        };

        return new ReportSection("Income Insights",
            [$"Total income: {Money(totalIncome)} from {bySource.Count} source(s)"],
            KpiCards: kpiCards,
            Insights: insights);
    }

    private ReportSection NetWorthBreakdownSection(ReportRequest request)
    {
        PortfolioSummary p = _assets.GetPortfolioSummary();
        var filter = new ExpenseFilter(From: request.From, To: request.To);
        decimal totalExpenses = _expenses.GetExpenses(filter).Sum(e => e.Amount);
        decimal totalIncome = _income.GetRecent(500).Sum(i => i.Amount);

        var chartPoints = new List<ReportChartPoint>
        {
            new("Net Worth", (double)p.TotalCurrentValue),
            new("Total Income", (double)totalIncome),
            new("Total Expenses", (double)totalExpenses),
        };

        return new ReportSection("Net Worth Breakdown",
            [$"Assets: {Money(p.TotalCurrentValue)} | Income: {Money(totalIncome)} | Expenses: {Money(totalExpenses)}",
             $"Net Surplus (Income − Expenses): {Money(totalIncome - totalExpenses)}"],
            new ReportTable(
                ["Metric", "Amount"],
                [
                    ["Asset Portfolio (Net Worth)", Money(p.TotalCurrentValue)],
                    ["Total Income (All Time)", Money(totalIncome)],
                    ["Total Expenses (All Time)", Money(totalExpenses)],
                    ["Net Surplus", Money(totalIncome - totalExpenses)],
                ]),
            new ReportChart(ReportChartKind.Column, chartPoints, IsLarge: true));
    }

    private ReportSection AssetTimelineSection()
    {
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();
        // Fetch full asset entities to get InvestmentStartDate
        var assetDates = assets
            .Select(a => (asset: a, full: _assets.GetById(a.Id)))
            .Where(x => x.full != null)
            .OrderBy(x => x.full!.InvestmentStartDate)
            .ToList();

        var rows = assetDates.Select((x, i) => (IReadOnlyList<string>)
        [
            $"#{i + 1}",
            x.full!.InvestmentStartDate.ToString("d", CultureInfo.CurrentCulture),
            x.asset.Name,
            x.asset.TypeName,
            Money(x.asset.InvestedAmount),
            Money(x.asset.CurrentValue),
        ]).ToList();

        return new ReportSection("Asset Acquisition Timeline",
            [$"{assets.Count} asset(s) tracked. Ordered by earliest acquisition date."],
            new ReportTable(
                ["#", "Date Added", "Asset", "Type", "Invested", "Current Value"],
                rows));
    }

    private ReportSection DocumentSummarySection()
    {
        IReadOnlyList<AssetSummary> assets = _assets.GetAssets();
        int totalDocs = 0, totalImages = 0, totalPdfs = 0, totalOther = 0;

        foreach (AssetSummary asset in assets)
        {
            var docs = _docs.GetForAsset(asset.Id);
            foreach (var doc in docs)
            {
                totalDocs++;
                switch (doc.Kind)
                {
                    case Domain.Assets.DocumentKind.Image: totalImages++; break;
                    case Domain.Assets.DocumentKind.Pdf:   totalPdfs++;   break;
                    default:                               totalOther++;  break;
                }
            }
        }

        var kpiCards = new List<ReportKpiCard>
        {
            new("Total Documents", totalDocs.ToString(), "Across all assets"),
            new("Images / Photos", totalImages.ToString(), "Asset photos"),
            new("PDF Documents", totalPdfs.ToString(), "Statements, certificates"),
            new("Other Files", totalOther.ToString(), "Contracts, misc"),
        };

        return new ReportSection("Attached Documents Overview",
            [$"Total: {totalDocs} document(s) across {assets.Count} asset(s)."],
            new ReportTable(
                ["Document Type", "Count"],
                [
                    ["Images / Photos", totalImages.ToString()],
                    ["PDF Documents", totalPdfs.ToString()],
                    ["Other Files", totalOther.ToString()],
                    ["Total", totalDocs.ToString()],
                ]),
            KpiCards: kpiCards);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════════

    private static string ScoreLabel(int score) => score switch
    {
        >= 75 => "Excellent",
        >= 50 => "Good",
        >= 25 => "Fair",
        _ => "Poor"
    };

    /// <summary>Decodes raw image bytes and resizes to a square thumbnail PNG via SkiaSharp.</summary>
    private static byte[] ResizeToThumbnail(byte[] raw, int size)
    {
        using var original = SKBitmap.Decode(raw);
        if (original is null) return raw;

        int w = original.Width, h = original.Height;
        float scale = Math.Min((float)size / w, (float)size / h);
        int nw = Math.Max(1, (int)(w * scale));
        int nh = Math.Max(1, (int)(h * scale));

        using var resized = original.Resize(new SKImageInfo(nw, nh), SKFilterQuality.Medium);
        if (resized is null) return raw;

        using var img = SKImage.FromBitmap(resized);
        using var data = img.Encode(SKEncodedImageFormat.Png, 85);
        return data.ToArray();
    }

    private static string Title(ReportType type) => type switch
    {
        ReportType.Assets => "Asset Report",
        ReportType.Expenses => "Expense Report",
        ReportType.Vault => "Password Vault Report",
        ReportType.Insurance => "Insurance Report",
        ReportType.FullPortfolio => "Full Portfolio Report",
        _ => "Report"
    };

    private static string Money(decimal value) => "₹" + value.ToString("N2", CultureInfo.CurrentCulture);
}

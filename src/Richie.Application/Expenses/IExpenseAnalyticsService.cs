using Richie.Domain.Expenses;

namespace Richie.Application.Expenses;

public enum AnalyticsPeriod { ThisMonth, ThisQuarter, ThisYear, AllTime }

public sealed record CategoryDatum(ExpenseCategory Category, string CategoryName, decimal Amount);

public sealed record PeriodDatum(string Label, decimal Amount);

/// <summary>Aggregations behind the expense analytics charts (PRD §7.4).</summary>
public interface IExpenseAnalyticsService
{
    IReadOnlyList<CategoryDatum> GetCategoryDistribution(AnalyticsPeriod period);
    IReadOnlyList<PeriodDatum> GetMonthlyTotals(int months = 12);
    IReadOnlyList<PeriodDatum> GetYearlyTotals();
}

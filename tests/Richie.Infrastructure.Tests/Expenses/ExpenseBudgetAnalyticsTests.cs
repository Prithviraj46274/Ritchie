using Richie.Application.Expenses;
using Richie.Domain.Expenses;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Expenses;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Expenses;

public sealed class ExpenseBudgetAnalyticsTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new(); // 2026-01-01
    private readonly UserSession _session = new();
    private readonly ExpenseService _expenses;
    private readonly ExpenseBudgetService _budgets;
    private readonly ExpenseAnalyticsService _analytics;

    public ExpenseBudgetAnalyticsTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _expenses = new ExpenseService(_db, _session, _clock);
        _budgets = new ExpenseBudgetService(_db, _session, _clock);
        _analytics = new ExpenseAnalyticsService(_db, _session, _clock);
    }

    private void Add(decimal amount, ExpenseCategory category, DateTime date) =>
        _expenses.Create(new ExpenseInput(date, amount, category, null, null, null));

    [Fact]
    public void Budget_Status_ReflectsActualVsLimit()
    {
        _budgets.SetBudgets(new Dictionary<ExpenseCategory, decimal>
        {
            [ExpenseCategory.DiningRestaurants] = 100,   // will be over
            [ExpenseCategory.GroceriesFood] = 100,       // within
        });

        Add(150, ExpenseCategory.DiningRestaurants, new DateTime(2026, 1, 10)); // 150% -> Over
        Add(50, ExpenseCategory.GroceriesFood, new DateTime(2026, 1, 10));      // 50% -> Good

        IReadOnlyList<BudgetRow> rows = _budgets.GetAnalysis();
        Assert.Equal(BudgetStatus.Over, rows.Single(r => r.Category == ExpenseCategory.DiningRestaurants).Status);
        Assert.Equal(BudgetStatus.Good, rows.Single(r => r.Category == ExpenseCategory.GroceriesFood).Status);
        Assert.Equal(BudgetStatus.Unset, rows.Single(r => r.Category == ExpenseCategory.Healthcare).Status);
        Assert.Equal(10, rows.Count); // all categories represented
    }

    [Fact]
    public void SetBudgets_WithZero_ClearsExisting()
    {
        _budgets.SetBudgets(new Dictionary<ExpenseCategory, decimal> { [ExpenseCategory.Transportation] = 200 });
        Assert.Equal(200m, _budgets.GetAnalysis().Single(r => r.Category == ExpenseCategory.Transportation).MonthlyLimit);

        _budgets.SetBudgets(new Dictionary<ExpenseCategory, decimal> { [ExpenseCategory.Transportation] = 0 });
        BudgetRow row = _budgets.GetAnalysis().Single(r => r.Category == ExpenseCategory.Transportation);
        Assert.Equal(0m, row.MonthlyLimit);
        Assert.Equal(BudgetStatus.Unset, row.Status);
    }

    [Fact]
    public void CategoryDistribution_RespectsPeriod()
    {
        Add(100, ExpenseCategory.DiningRestaurants, new DateTime(2026, 1, 5));  // this month
        Add(40, ExpenseCategory.Transportation, new DateTime(2026, 1, 6));      // this month
        Add(500, ExpenseCategory.Healthcare, new DateTime(2025, 6, 1));         // last year

        IReadOnlyList<CategoryDatum> month = _analytics.GetCategoryDistribution(AnalyticsPeriod.ThisMonth);
        Assert.Equal(2, month.Count);
        Assert.Equal(ExpenseCategory.DiningRestaurants, month[0].Category); // ordered by amount desc

        Assert.Equal(3, _analytics.GetCategoryDistribution(AnalyticsPeriod.AllTime).Count);
    }

    [Fact]
    public void MonthlyTotals_ReturnsRollingWindowEndingThisMonth()
    {
        Add(100, ExpenseCategory.GroceriesFood, new DateTime(2026, 1, 5));
        Add(60, ExpenseCategory.GroceriesFood, new DateTime(2025, 12, 5));

        IReadOnlyList<PeriodDatum> monthly = _analytics.GetMonthlyTotals(12);
        Assert.Equal(12, monthly.Count);
        Assert.Equal(100m, monthly[^1].Amount);  // current month (Jan 2026)
        Assert.Equal(60m, monthly[^2].Amount);   // previous month (Dec 2025)
    }

    [Fact]
    public void YearlyTotals_AggregatesPerYear()
    {
        Add(100, ExpenseCategory.GroceriesFood, new DateTime(2025, 3, 1));
        Add(50, ExpenseCategory.GroceriesFood, new DateTime(2025, 9, 1));
        Add(200, ExpenseCategory.GroceriesFood, new DateTime(2026, 1, 1));

        IReadOnlyList<PeriodDatum> yearly = _analytics.GetYearlyTotals();
        Assert.Equal(2, yearly.Count);
        Assert.Equal("2025", yearly[0].Label);
        Assert.Equal(150m, yearly[0].Amount);
        Assert.Equal(200m, yearly[1].Amount);
    }

    public void Dispose() => _db.Dispose();
}

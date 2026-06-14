using Richie.Domain.Expenses;

namespace Richie.Application.Expenses;

public enum BudgetStatus { Unset, Good, Warning, Over }

public sealed record BudgetRow(
    ExpenseCategory Category, string CategoryName, decimal MonthlyLimit,
    decimal ActualThisMonth, decimal Percent, BudgetStatus Status);

/// <summary>Per-category monthly budgets and this-month actual-vs-budget analysis (PRD §7.4/§15).</summary>
public interface IExpenseBudgetService
{
    /// <summary>One row per category (all 10), with this month's actual and status.</summary>
    IReadOnlyList<BudgetRow> GetAnalysis();

    /// <summary>Upserts the given monthly limits (a limit ≤ 0 clears that category's budget).</summary>
    void SetBudgets(IReadOnlyDictionary<ExpenseCategory, decimal> limits);
}

using Richie.Application.Expenses;

namespace Richie.Application.Income;

public sealed record IncomeInput(DateTime Date, decimal Amount, string Source, string? Notes);

public sealed record IncomeSummary(Guid Id, DateTime Date, decimal Amount, string Source);

/// <summary>Income CRUD (user-scoped, audited) + monthly aggregates for the income-vs-expense view.</summary>
public interface IIncomeService
{
    IReadOnlyList<IncomeSummary> GetRecent(int count = 100);
    IncomeInput? GetById(Guid id);
    Guid Create(IncomeInput input);
    bool Update(Guid id, IncomeInput input);
    bool Delete(Guid id);

    /// <summary>Total income for the current calendar month.</summary>
    decimal GetMonthlyTotal();

    /// <summary>Income totals for the trailing <paramref name="months"/> months (zero-filled),
    /// labelled identically to the expense monthly totals so the two can be charted together.</summary>
    IReadOnlyList<PeriodDatum> GetMonthlyTotals(int months = 6);
}

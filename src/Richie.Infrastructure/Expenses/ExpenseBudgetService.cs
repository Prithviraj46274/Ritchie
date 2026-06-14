using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Expenses;
using Richie.Domain.Auditing;
using Richie.Domain.Expenses;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Expenses;

public sealed class ExpenseBudgetService : IExpenseBudgetService
{
    private const string Module = "Expenses";

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public ExpenseBudgetService(IAppDbContextFactory factory, IUserSession session, IClock clock)
    {
        _factory = factory;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<BudgetRow> GetAnalysis()
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();

        Dictionary<ExpenseCategory, decimal> limits = db.ExpenseBudgets.AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToDictionary(b => b.Category, b => b.MonthlyLimit);

        Dictionary<ExpenseCategory, decimal> actuals = db.Expenses.AsNoTracking()
            .Where(e => e.UserId == userId)
            .AsEnumerable()
            .Where(e => e.Date.Year == now.Year && e.Date.Month == now.Month)
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var rows = new List<BudgetRow>();
        foreach (ExpenseCategory category in Enum.GetValues<ExpenseCategory>())
        {
            decimal limit = limits.GetValueOrDefault(category, 0);
            decimal actual = actuals.GetValueOrDefault(category, 0);
            decimal percent = limit > 0 ? Math.Round(actual / limit * 100, 1) : 0;
            BudgetStatus status = limit <= 0 ? BudgetStatus.Unset
                : percent <= 80 ? BudgetStatus.Good
                : percent <= 100 ? BudgetStatus.Warning
                : BudgetStatus.Over;

            rows.Add(new BudgetRow(category, ExpenseCategoryNames.Display(category), limit, actual, percent, status));
        }
        return rows;
    }

    public void SetBudgets(IReadOnlyDictionary<ExpenseCategory, decimal> limits)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();

        List<ExpenseBudget> existing = db.ExpenseBudgets.Where(b => b.UserId == userId).ToList();

        foreach ((ExpenseCategory category, decimal limit) in limits)
        {
            ExpenseBudget? row = existing.FirstOrDefault(b => b.Category == category);
            if (limit <= 0)
            {
                if (row is not null)
                    db.ExpenseBudgets.Remove(row);
                continue;
            }

            if (row is null)
                db.ExpenseBudgets.Add(new ExpenseBudget
                {
                    Id = Guid.NewGuid(), UserId = userId, Category = category, MonthlyLimit = limit, UpdatedUtc = now
                });
            else
            {
                row.MonthlyLimit = limit;
                row.UpdatedUtc = now;
            }
        }

        AuditWriter.Add(db, userId, now, Module, AuditAction.Update, nameof(ExpenseBudget), userId, "Updated budget targets.");
        db.SaveChanges();
    }
}

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Expenses;
using Richie.Domain.Expenses;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Expenses;

public sealed class ExpenseAnalyticsService : IExpenseAnalyticsService
{
    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public ExpenseAnalyticsService(IAppDbContextFactory factory, IUserSession session, IClock clock)
    {
        _factory = factory;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    private List<Expense> LoadAll(RichieDbContext db, Guid userId) =>
        db.Expenses.AsNoTracking().Where(e => e.UserId == userId).ToList();

    public IReadOnlyList<CategoryDatum> GetCategoryDistribution(AnalyticsPeriod period)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();

        IEnumerable<Expense> scope = LoadAll(db, userId).Where(e => InPeriod(e.Date, period, now));
        return scope
            .GroupBy(e => e.Category)
            .Select(g => new CategoryDatum(g.Key, ExpenseCategoryNames.Display(g.Key), g.Sum(e => e.Amount)))
            .Where(d => d.Amount > 0)
            .OrderByDescending(d => d.Amount)
            .ToList();
    }

    public IReadOnlyList<PeriodDatum> GetMonthlyTotals(int months = 12)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        List<Expense> all = LoadAll(db, userId);

        var result = new List<PeriodDatum>(months);
        for (int i = months - 1; i >= 0; i--)
        {
            DateTime month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            decimal total = all.Where(e => e.Date.Year == month.Year && e.Date.Month == month.Month).Sum(e => e.Amount);
            result.Add(new PeriodDatum(month.ToString("MMM yyyy", CultureInfo.CurrentCulture), total));
        }
        return result;
    }

    public IReadOnlyList<PeriodDatum> GetYearlyTotals()
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return LoadAll(db, userId)
            .GroupBy(e => e.Date.Year)
            .OrderBy(g => g.Key)
            .Select(g => new PeriodDatum(g.Key.ToString(CultureInfo.InvariantCulture), g.Sum(e => e.Amount)))
            .ToList();
    }

    private static bool InPeriod(DateTime date, AnalyticsPeriod period, DateTime now) => period switch
    {
        AnalyticsPeriod.ThisMonth => date.Year == now.Year && date.Month == now.Month,
        AnalyticsPeriod.ThisQuarter => date.Year == now.Year && Quarter(date.Month) == Quarter(now.Month),
        AnalyticsPeriod.ThisYear => date.Year == now.Year,
        _ => true
    };

    private static int Quarter(int month) => (month - 1) / 3;
}

using Richie.Application.Expenses;
using Richie.Domain.Expenses;
using Richie.Domain.Notifications;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Expenses;
using Richie.Infrastructure.Notifications;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Expenses;

public sealed class ExpenseRecurringServiceTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new(); // 2026-01-01
    private readonly UserSession _session = new();
    private readonly ExpenseService _expenses;
    private readonly ExpenseRecurringService _sut;

    public ExpenseRecurringServiceTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _expenses = new ExpenseService(_db, _session, _clock);
        _sut = new ExpenseRecurringService(_db, _session, _clock);
    }

    private Guid AddRule(ExpenseRecurringFrequency freq, DateTime start, DateTime? end = null, decimal amount = 100) =>
        _sut.CreateRule(new RecurringInput(true, amount, ExpenseCategory.HousingUtilities, "Me", "Rent", null, freq, start, end));

    [Fact]
    public void ProcessDueRecurring_GeneratesDueEntry_TaggedAndNotified()
    {
        AddRule(ExpenseRecurringFrequency.Monthly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        int generated = _sut.ProcessDueRecurring(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, generated);
        ExpenseSummary expense = Assert.Single(_expenses.GetExpenses());
        Assert.True(expense.IsRecurring);

        var notifications = new NotificationService(_db, _session).GetRecent();
        Assert.Contains(notifications, n => n.Type == NotificationType.RecurringExpense);
    }

    [Fact]
    public void ProcessDueRecurring_CatchesUpMonthly()
    {
        AddRule(ExpenseRecurringFrequency.Monthly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Jan, Feb, Mar due as of Mar 15.
        int generated = _sut.ProcessDueRecurring(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, generated);
        Assert.Equal(3, _expenses.GetExpenses().Count);
    }

    [Fact]
    public void ProcessDueRecurring_WeeklyAdvancesBySevenDays()
    {
        AddRule(ExpenseRecurringFrequency.Weekly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Jan 1, 8, 15 due as of Jan 20.
        Assert.Equal(3, _sut.ProcessDueRecurring(new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void ProcessDueRecurring_StopsAtEndDate()
    {
        AddRule(ExpenseRecurringFrequency.Monthly,
            start: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            end: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        // Without an end date this would be 6 entries; end date caps it at Jan + Feb.
        int generated = _sut.ProcessDueRecurring(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, generated);
    }

    [Fact]
    public void ProcessDueRecurring_IgnoresDisabledRules()
    {
        _sut.CreateRule(new RecurringInput(false, 100, ExpenseCategory.HousingUtilities, null, null, null,
            ExpenseRecurringFrequency.Monthly, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null));

        Assert.Equal(0, _sut.ProcessDueRecurring(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Empty(_expenses.GetExpenses());
    }

    public void Dispose() => _db.Dispose();
}

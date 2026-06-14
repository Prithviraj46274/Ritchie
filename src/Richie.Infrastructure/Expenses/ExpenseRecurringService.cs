using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Expenses;
using Richie.Domain.Auditing;
using Richie.Domain.Expenses;
using Richie.Domain.Notifications;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Notifications;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Expenses;

public sealed class ExpenseRecurringService : IExpenseRecurringService
{
    private const string Module = "Expenses";

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public ExpenseRecurringService(IAppDbContextFactory factory, IUserSession session, IClock clock)
    {
        _factory = factory;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<RecurringSummary> GetRules()
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.ExpenseRecurrings.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.IsEnabled).ThenBy(r => r.NextRunDateUtc)
            .AsEnumerable()
            .Select(r => new RecurringSummary(r.Id, r.IsEnabled, r.Amount, r.Category,
                ExpenseCategoryNames.Display(r.Category), r.Frequency, r.StartDate, r.EndDate, r.NextRunDateUtc))
            .ToList();
    }

    public RecurringInput? GetRule(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        ExpenseRecurring? r = db.ExpenseRecurrings.AsNoTracking().FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return r is null
            ? null
            : new RecurringInput(r.IsEnabled, r.Amount, r.Category, r.SpentBy, r.SpentFor, r.Notes, r.Frequency, r.StartDate, r.EndDate);
    }

    public Guid CreateRule(RecurringInput input)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;

        var rule = new ExpenseRecurring
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        Apply(rule, input);

        using RichieDbContext db = _factory.Create();
        db.ExpenseRecurrings.Add(rule);
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(ExpenseRecurring), rule.Id,
            $"Added recurring {ExpenseCategoryNames.Display(rule.Category)} expense.");
        db.SaveChanges();
        return rule.Id;
    }

    public bool UpdateRule(Guid id, RecurringInput input)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        ExpenseRecurring? rule = db.ExpenseRecurrings.FirstOrDefault(r => r.Id == id && r.UserId == userId);
        if (rule is null)
            return false;

        Apply(rule, input);
        rule.UpdatedUtc = _clock.UtcNow;
        AuditWriter.Add(db, userId, rule.UpdatedUtc, Module, AuditAction.Update, nameof(ExpenseRecurring), rule.Id,
            "Updated recurring expense.");
        db.SaveChanges();
        return true;
    }

    public bool DeleteRule(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        ExpenseRecurring? rule = db.ExpenseRecurrings.FirstOrDefault(r => r.Id == id && r.UserId == userId);
        if (rule is null)
            return false;

        db.ExpenseRecurrings.Remove(rule);
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(ExpenseRecurring), rule.Id,
            "Deleted recurring expense.");
        db.SaveChanges();
        return true;
    }

    public int ProcessDueRecurring(DateTime nowUtc)
    {
        using RichieDbContext db = _factory.Create();
        List<ExpenseRecurring> due = db.ExpenseRecurrings
            .Where(r => r.IsEnabled && r.NextRunDateUtc <= nowUtc)
            .ToList();

        int generated = 0;
        foreach (ExpenseRecurring rule in due)
        {
            while (rule.NextRunDateUtc <= nowUtc && (rule.EndDate is null || rule.NextRunDateUtc <= rule.EndDate))
            {
                db.Expenses.Add(new Expense
                {
                    Id = Guid.NewGuid(),
                    UserId = rule.UserId,
                    Date = rule.NextRunDateUtc,
                    Amount = rule.Amount,
                    Category = rule.Category,
                    SpentBy = rule.SpentBy,
                    SpentFor = rule.SpentFor,
                    Notes = rule.Notes,
                    RecurringId = rule.Id,
                    CreatedUtc = nowUtc,
                    UpdatedUtc = nowUtc
                });

                NotificationWriter.Add(db, rule.UserId, nowUtc, NotificationType.RecurringExpense,
                    "Recurring expense added",
                    $"A {ExpenseCategoryNames.Display(rule.Category)} expense was recorded automatically.");
                AuditWriter.Add(db, rule.UserId, nowUtc, Module, AuditAction.Create, nameof(Expense), rule.Id,
                    $"Auto-generated recurring {ExpenseCategoryNames.Display(rule.Category)} expense.");

                rule.LastRunUtc = nowUtc;
                rule.NextRunDateUtc = Advance(rule.NextRunDateUtc, rule.Frequency);
                generated++;
            }
        }

        db.SaveChanges();
        return generated;
    }

    private static void Apply(ExpenseRecurring rule, RecurringInput input)
    {
        rule.IsEnabled = input.IsEnabled;
        rule.Amount = input.Amount;
        rule.Category = input.Category;
        rule.SpentBy = string.IsNullOrWhiteSpace(input.SpentBy) ? null : input.SpentBy.Trim();
        rule.SpentFor = string.IsNullOrWhiteSpace(input.SpentFor) ? null : input.SpentFor.Trim();
        rule.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();
        rule.Frequency = input.Frequency;
        rule.StartDate = input.StartDate;
        rule.EndDate = input.EndDate;
        // The first occurrence is the start date; past start dates are caught up on the next run.
        rule.NextRunDateUtc = input.StartDate;
    }

    private static DateTime Advance(DateTime from, ExpenseRecurringFrequency frequency) => frequency switch
    {
        ExpenseRecurringFrequency.Weekly => from.AddDays(7),
        ExpenseRecurringFrequency.Monthly => from.AddMonths(1),
        ExpenseRecurringFrequency.Quarterly => from.AddMonths(3),
        ExpenseRecurringFrequency.Annually => from.AddYears(1),
        _ => from.AddMonths(1)
    };
}

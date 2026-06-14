using Richie.Domain.Expenses;

namespace Richie.Application.Expenses;

public sealed record RecurringInput(
    bool IsEnabled, decimal Amount, ExpenseCategory Category, string? SpentBy, string? SpentFor, string? Notes,
    ExpenseRecurringFrequency Frequency, DateTime StartDate, DateTime? EndDate);

public sealed record RecurringSummary(
    Guid Id, bool IsEnabled, decimal Amount, ExpenseCategory Category, string CategoryName,
    ExpenseRecurringFrequency Frequency, DateTime StartDate, DateTime? EndDate, DateTime NextRunDateUtc);

/// <summary>
/// Recurring-expense rules and the automation that auto-generates due entries (PRD §7.5).
/// </summary>
public interface IExpenseRecurringService
{
    IReadOnlyList<RecurringSummary> GetRules();
    RecurringInput? GetRule(Guid id);
    Guid CreateRule(RecurringInput input);
    bool UpdateRule(Guid id, RecurringInput input);
    bool DeleteRule(Guid id);

    /// <summary>
    /// Generates every entry due at or before <paramref name="nowUtc"/> across ALL users
    /// (run by the background service), honouring each rule's end date. Returns the number generated.
    /// </summary>
    int ProcessDueRecurring(DateTime nowUtc);
}

namespace Richie.Domain.Expenses;

public enum ExpenseRecurringFrequency
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Annually = 4
}

/// <summary>
/// A rule that auto-generates expense entries on a schedule (PRD §7.5). Generated
/// <see cref="Expense"/> rows carry this rule's id in <see cref="Expense.RecurringId"/>.
/// </summary>
public class ExpenseRecurring
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public bool IsEnabled { get; set; }
    public decimal Amount { get; set; }
    public ExpenseCategory Category { get; set; }
    public string? SpentBy { get; set; }
    public string? SpentFor { get; set; }
    public string? Notes { get; set; }

    public ExpenseRecurringFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextRunDateUtc { get; set; }
    public DateTime? LastRunUtc { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

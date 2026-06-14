namespace Richie.Domain.Expenses;

/// <summary>A user's monthly spending budget for one expense category (PRD §15).</summary>
public class ExpenseBudget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal MonthlyLimit { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

namespace Richie.Domain.Income;

/// <summary>A single income entry (salary, freelance, interest, etc.). Tracked alongside expenses
/// so the Expense Tracker can show income vs spending.</summary>
public class Income
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Free-text source/label, e.g. "Salary", "Freelance", "Interest".</summary>
    public string Source { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

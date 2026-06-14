namespace Richie.Domain.Assets;

public enum GoalPriority
{
    High = 1,
    Medium = 2,
    Low = 3
}

/// <summary>
/// A financial goal owned by the Asset module (PRD §6.9). Progress is derived from the
/// current value of the assets linked to it via <see cref="AssetGoalLink"/>.
/// </summary>
public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public DateTime TargetDate { get; set; }
    public GoalPriority Priority { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

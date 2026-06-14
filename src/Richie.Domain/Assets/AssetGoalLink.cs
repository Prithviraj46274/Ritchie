namespace Richie.Domain.Assets;

/// <summary>Many-to-many link: an asset contributes to a goal (PRD §19.2 AssetGoalLinks).</summary>
public class AssetGoalLink
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }
    public Guid AssetId { get; set; }
}

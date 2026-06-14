namespace Richie.Application.Assets;

/// <summary>
/// Financial goals and their linked assets (PRD §6.9). Progress is derived from linked assets'
/// current value; projected completion uses the linked assets' active SIP rate.
/// </summary>
public interface IGoalService
{
    IReadOnlyList<GoalProgress> GetGoals();
    GoalEditData? GetGoal(Guid id);

    Guid CreateGoal(GoalInput input, IReadOnlyList<Guid> linkedAssetIds);
    bool UpdateGoal(Guid id, GoalInput input, IReadOnlyList<Guid> linkedAssetIds);
    bool DeleteGoal(Guid id);

    /// <summary>Names of goals an asset is linked to (for the asset details "goal tags").</summary>
    IReadOnlyList<string> GetGoalNamesForAsset(Guid assetId);
}

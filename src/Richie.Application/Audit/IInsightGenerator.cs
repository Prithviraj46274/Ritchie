namespace Richie.Application.Audit;

/// <summary>
/// Produces the cross-module "what does this mean for me?" text insights (PRD §18.1) by combining
/// spending trends and portfolio/coverage findings. Consumed by the Dashboard (Phase 7) and module
/// home screens. (Vault-health insights are added once the vault is unlocked.)
/// </summary>
public interface IInsightGenerator
{
    IReadOnlyList<string> Generate(int max = 8);
}

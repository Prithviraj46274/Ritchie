using Richie.Domain.Assets;

namespace Richie.Application.Assets;

public sealed record GoalInput(string Name, decimal TargetAmount, DateTime TargetDate, GoalPriority Priority);

/// <summary>A goal with its linked-asset ids — used to prefill the edit form.</summary>
public sealed record GoalEditData(
    string Name, decimal TargetAmount, DateTime TargetDate, GoalPriority Priority,
    IReadOnlyList<Guid> LinkedAssetIds);

/// <summary>
/// Goal progress. <see cref="PercentComplete"/> = current value ÷ target (capped at 100).
/// <see cref="ProjectedCompletionUtc"/> is derived from the linked enabled SIPs' monthly rate
/// (null when there is no active SIP backing the goal).
/// </summary>
public sealed record GoalProgress(
    Guid Id,
    string Name,
    GoalPriority Priority,
    decimal TargetAmount,
    decimal CurrentValue,
    decimal Gap,
    decimal PercentComplete,
    DateTime TargetDate,
    decimal MonthlyContribution,
    DateTime? ProjectedCompletionUtc);

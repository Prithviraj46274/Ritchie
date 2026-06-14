using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Assets;
using Richie.Application.Authentication;
using Richie.Domain.Assets;
using Richie.Domain.Auditing;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Assets;

public sealed class GoalService : IGoalService
{
    private const string Module = "Assets";
    private const int MaxProjectionMonths = 1200;

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public GoalService(IAppDbContextFactory factory, IUserSession session, IClock clock)
    {
        _factory = factory;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<GoalProgress> GetGoals()
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();

        List<Goal> goals = db.Goals.AsNoTracking().Where(g => g.UserId == userId).OrderBy(g => g.Priority).ToList();
        var results = new List<GoalProgress>(goals.Count);

        foreach (Goal goal in goals)
        {
            List<Guid> assetIds = db.AssetGoalLinks.AsNoTracking()
                .Where(l => l.GoalId == goal.Id).Select(l => l.AssetId).ToList();

            decimal current = db.Assets.AsNoTracking()
                .Where(a => assetIds.Contains(a.Id)).Sum(a => a.CurrentValue);

            decimal monthly = db.SipSchedules.AsNoTracking()
                .Where(s => s.IsEnabled && assetIds.Contains(s.AssetId))
                .AsEnumerable()
                .Sum(s => s.Frequency == SipFrequency.Monthly ? s.Amount : s.Amount / 3m);

            decimal gap = Math.Max(0, goal.TargetAmount - current);
            decimal percent = goal.TargetAmount > 0
                ? Math.Min(100, Math.Round(current / goal.TargetAmount * 100, 1))
                : 0;

            DateTime? projected = ComputeProjection(now, gap, monthly);

            results.Add(new GoalProgress(goal.Id, goal.Name, goal.Priority, goal.TargetAmount,
                current, gap, percent, goal.TargetDate, monthly, projected));
        }

        return results;
    }

    public GoalEditData? GetGoal(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Goal? goal = db.Goals.AsNoTracking().FirstOrDefault(g => g.Id == id && g.UserId == userId);
        if (goal is null)
            return null;

        List<Guid> assetIds = db.AssetGoalLinks.AsNoTracking()
            .Where(l => l.GoalId == id).Select(l => l.AssetId).ToList();

        return new GoalEditData(goal.Name, goal.TargetAmount, goal.TargetDate, goal.Priority, assetIds);
    }

    public Guid CreateGoal(GoalInput input, IReadOnlyList<Guid> linkedAssetIds)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();

        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = input.Name.Trim(),
            TargetAmount = input.TargetAmount,
            TargetDate = input.TargetDate,
            Priority = input.Priority,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        db.Goals.Add(goal);
        AddLinks(db, goal.Id, userId, linkedAssetIds);

        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(Goal), goal.Id, $"Added goal '{goal.Name}'.");
        db.SaveChanges();
        return goal.Id;
    }

    public bool UpdateGoal(Guid id, GoalInput input, IReadOnlyList<Guid> linkedAssetIds)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Goal? goal = db.Goals.FirstOrDefault(g => g.Id == id && g.UserId == userId);
        if (goal is null)
            return false;

        goal.Name = input.Name.Trim();
        goal.TargetAmount = input.TargetAmount;
        goal.TargetDate = input.TargetDate;
        goal.Priority = input.Priority;
        goal.UpdatedUtc = _clock.UtcNow;

        db.AssetGoalLinks.RemoveRange(db.AssetGoalLinks.Where(l => l.GoalId == id));
        AddLinks(db, id, userId, linkedAssetIds);

        AuditWriter.Add(db, userId, goal.UpdatedUtc, Module, AuditAction.Update, nameof(Goal), goal.Id, $"Updated goal '{goal.Name}'.");
        db.SaveChanges();
        return true;
    }

    public bool DeleteGoal(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Goal? goal = db.Goals.FirstOrDefault(g => g.Id == id && g.UserId == userId);
        if (goal is null)
            return false;

        db.Goals.Remove(goal); // links cascade
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(Goal), goal.Id, $"Deleted goal '{goal.Name}'.");
        db.SaveChanges();
        return true;
    }

    public IReadOnlyList<string> GetGoalNamesForAsset(Guid assetId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        List<Guid> goalIds = db.AssetGoalLinks.AsNoTracking()
            .Where(l => l.AssetId == assetId).Select(l => l.GoalId).ToList();

        return db.Goals.AsNoTracking()
            .Where(g => g.UserId == userId && goalIds.Contains(g.Id))
            .OrderBy(g => g.Name)
            .Select(g => g.Name)
            .ToList();
    }

    private static void AddLinks(RichieDbContext db, Guid goalId, Guid userId, IReadOnlyList<Guid> assetIds)
    {
        // Only link assets that belong to the user.
        HashSet<Guid> owned = db.Assets.Where(a => a.UserId == userId && assetIds.Contains(a.Id))
            .Select(a => a.Id).ToHashSet();

        foreach (Guid assetId in owned)
            db.AssetGoalLinks.Add(new AssetGoalLink { Id = Guid.NewGuid(), GoalId = goalId, AssetId = assetId });
    }

    private static DateTime? ComputeProjection(DateTime now, decimal gap, decimal monthlyContribution)
    {
        if (gap <= 0)
            return now;
        if (monthlyContribution <= 0)
            return null;

        int months = (int)Math.Ceiling(gap / monthlyContribution);
        return months > MaxProjectionMonths ? null : now.Date.AddMonths(months);
    }
}

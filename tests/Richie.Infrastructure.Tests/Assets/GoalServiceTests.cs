using Richie.Application.Assets;
using Richie.Domain.Assets;
using Richie.Infrastructure.Assets;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Assets;

public sealed class GoalServiceTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new(); // 2026-01-01
    private readonly UserSession _session = new();
    private readonly AssetService _assets;
    private readonly SipService _sip;
    private readonly GoalService _sut;

    public GoalServiceTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _assets = new AssetService(_db, new ValuationService(), _session, _clock);
        _sip = new SipService(_db, _session, _clock);
        _sut = new GoalService(_db, _session, _clock);
    }

    private Guid CreateAsset(decimal current) => _assets.Create(new AssetInput
    {
        Type = AssetType.MutualFund,
        Name = "Fund",
        InvestmentStartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        InvestedAmount = current,
        CurrentValue = current,
        InvestmentMode = InvestmentMode.Sip,
    });

    private static GoalInput Goal(string name = "Retirement", decimal target = 10000) =>
        new(name, target, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), GoalPriority.High);

    [Fact]
    public void CreateGoal_WithLinkedAssets_ComputesProgress()
    {
        Guid a1 = CreateAsset(3000);
        Guid a2 = CreateAsset(1000);

        _sut.CreateGoal(Goal(target: 10000), [a1, a2]);

        GoalProgress progress = Assert.Single(_sut.GetGoals());
        Assert.Equal(4000m, progress.CurrentValue);
        Assert.Equal(6000m, progress.Gap);
        Assert.Equal(40m, progress.PercentComplete);
    }

    [Fact]
    public void Progress_ProjectsCompletion_FromLinkedSipRate()
    {
        Guid asset = CreateAsset(0);            // current 0, target 12000, gap 12000
        _sip.SaveSchedule(asset, new SipScheduleInput(true, 1000, 15, SipFrequency.Monthly,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        _sut.CreateGoal(Goal(target: 12000), [asset]);

        GoalProgress p = Assert.Single(_sut.GetGoals());
        Assert.Equal(1000m, p.MonthlyContribution);
        // 12000 gap / 1000 per month = 12 months from 2026-01-01.
        Assert.Equal(new DateTime(2027, 1, 1), p.ProjectedCompletionUtc);
    }

    [Fact]
    public void Progress_HasNoProjection_WithoutActiveSip()
    {
        Guid asset = CreateAsset(1000);
        _sut.CreateGoal(Goal(target: 5000), [asset]);

        GoalProgress p = Assert.Single(_sut.GetGoals());
        Assert.Null(p.ProjectedCompletionUtc);
    }

    [Fact]
    public void UpdateGoal_ReplacesLinks()
    {
        Guid a1 = CreateAsset(1000);
        Guid a2 = CreateAsset(2000);
        Guid goalId = _sut.CreateGoal(Goal(target: 10000), [a1]);

        Assert.True(_sut.UpdateGoal(goalId, Goal(name: "Home", target: 10000), [a2]));

        GoalEditData edit = _sut.GetGoal(goalId)!;
        Assert.Equal("Home", edit.Name);
        Assert.Equal([a2], edit.LinkedAssetIds);
        Assert.Equal(2000m, _sut.GetGoals().Single().CurrentValue);
    }

    [Fact]
    public void GetGoalNamesForAsset_ReturnsLinkedGoals()
    {
        Guid asset = CreateAsset(1000);
        _sut.CreateGoal(Goal(name: "Retirement"), [asset]);
        _sut.CreateGoal(Goal(name: "Travel"), [asset]);

        IReadOnlyList<string> names = _sut.GetGoalNamesForAsset(asset);
        Assert.Equal(2, names.Count);
        Assert.Contains("Retirement", names);
        Assert.Contains("Travel", names);
    }

    [Fact]
    public void DeleteGoal_RemovesIt()
    {
        Guid goalId = _sut.CreateGoal(Goal(), []);
        Assert.True(_sut.DeleteGoal(goalId));
        Assert.Empty(_sut.GetGoals());
    }

    public void Dispose() => _db.Dispose();
}

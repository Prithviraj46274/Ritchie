using Richie.Application.Income;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Income;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Income;

public sealed class IncomeServiceTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new();
    private readonly UserSession _session = new();
    private readonly IncomeService _sut;

    public IncomeServiceTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _sut = new IncomeService(_db, _session, _clock);
    }

    [Fact]
    public void Create_CountsTowardCurrentMonthTotal()
    {
        _sut.Create(new IncomeInput(_clock.UtcNow, 5000m, "Salary", null));
        _sut.Create(new IncomeInput(_clock.UtcNow, 1000m, "Freelance", null));
        _sut.Create(new IncomeInput(_clock.UtcNow.AddMonths(-2), 999m, "Old", null)); // different month

        Assert.Equal(6000m, _sut.GetMonthlyTotal());
        Assert.Equal(3, _sut.GetRecent().Count);
    }

    [Fact]
    public void GetMonthlyTotals_ReturnsRequestedWindow_WithCurrentMonthLast()
    {
        _sut.Create(new IncomeInput(_clock.UtcNow, 5000m, "Salary", null));

        IReadOnlyList<Richie.Application.Expenses.PeriodDatum> totals = _sut.GetMonthlyTotals(6);

        Assert.Equal(6, totals.Count);
        Assert.Equal(5000m, totals[^1].Amount);   // current month is last
    }

    [Fact]
    public void Update_Delete_And_Scoping_Work()
    {
        Guid id = _sut.Create(new IncomeInput(_clock.UtcNow, 100m, "X", null));
        Assert.True(_sut.Update(id, new IncomeInput(_clock.UtcNow, 200m, "X", null)));
        Assert.Equal(200m, _sut.GetMonthlyTotal());

        _session.SignOut();
        _session.SignIn(Guid.NewGuid(), "Other");
        Assert.Equal(0m, _sut.GetMonthlyTotal());
        Assert.False(_sut.Delete(id));
    }

    public void Dispose() => _db.Dispose();
}

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Expenses;
using Richie.Application.Income;
using Richie.Domain.Auditing;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;
using IncomeEntity = Richie.Domain.Income.Income;

namespace Richie.Infrastructure.Income;

public sealed class IncomeService : IIncomeService
{
    private const string Module = "Income";

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IClock _clock;

    public IncomeService(IAppDbContextFactory factory, IUserSession session, IClock clock)
    {
        _factory = factory;
        _session = session;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<IncomeSummary> GetRecent(int count = 100)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.Incomes.AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.Date)
            .Take(count)
            .Select(i => new IncomeSummary(i.Id, i.Date, i.Amount, i.Source))
            .ToList();
    }

    public IncomeInput? GetById(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        IncomeEntity? i = db.Incomes.AsNoTracking().FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return i is null ? null : new IncomeInput(i.Date, i.Amount, i.Source, i.Notes);
    }

    public Guid Create(IncomeInput input)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;

        var income = new IncomeEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = input.Date,
            Amount = input.Amount,
            Source = input.Source.Trim(),
            Notes = Trim(input.Notes),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        using RichieDbContext db = _factory.Create();
        db.Incomes.Add(income);
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(IncomeEntity), income.Id,
            $"Added income '{income.Source}'.");
        db.SaveChanges();
        return income.Id;
    }

    public bool Update(Guid id, IncomeInput input)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        IncomeEntity? i = db.Incomes.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (i is null)
            return false;

        i.Date = input.Date;
        i.Amount = input.Amount;
        i.Source = input.Source.Trim();
        i.Notes = Trim(input.Notes);
        i.UpdatedUtc = _clock.UtcNow;
        AuditWriter.Add(db, userId, i.UpdatedUtc, Module, AuditAction.Update, nameof(IncomeEntity), i.Id, "Updated income.");
        db.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        IncomeEntity? i = db.Incomes.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (i is null)
            return false;

        db.Incomes.Remove(i);
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(IncomeEntity), i.Id, "Deleted income.");
        db.SaveChanges();
        return true;
    }

    public decimal GetMonthlyTotal()
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        return db.Incomes.AsNoTracking()
            .Where(i => i.UserId == userId)
            .ToList()
            .Where(i => i.Date.Year == now.Year && i.Date.Month == now.Month)
            .Sum(i => i.Amount);
    }

    public IReadOnlyList<PeriodDatum> GetMonthlyTotals(int months = 6)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        List<IncomeEntity> all = db.Incomes.AsNoTracking().Where(i => i.UserId == userId).ToList();

        var result = new List<PeriodDatum>(months);
        for (int i = months - 1; i >= 0; i--)
        {
            DateTime month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            decimal total = all.Where(x => x.Date.Year == month.Year && x.Date.Month == month.Month).Sum(x => x.Amount);
            result.Add(new PeriodDatum(month.ToString("MMM yyyy", CultureInfo.CurrentCulture), total));
        }
        return result;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

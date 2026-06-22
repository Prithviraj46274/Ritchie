using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Assets;
using Richie.Application.Authentication;
using Richie.Application.Liabilities;
using Richie.Domain.Auditing;
using Richie.Domain.Liabilities;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Liabilities;

public sealed class LoanService : ILoanService
{
    private const string Module = "Liabilities";
    private const decimal HighInterestThresholdPercent = 14m;

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IClock _clock;
    private readonly IAssetService _assets;

    public LoanService(IAppDbContextFactory factory, IUserSession session, IClock clock, IAssetService assets)
    {
        _factory = factory;
        _session = session;
        _clock = clock;
        _assets = assets;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<LoanSummary> GetLoans()
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        return db.Loans.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.NextDueDate ?? DateTime.MaxValue)
            .ToList()
            .Select(l => ToSummary(l, now))
            .ToList();
    }

    public LoanInput? GetById(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Loan? l = db.Loans.AsNoTracking().FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return l is null ? null : ToInput(l);
    }

    public Guid Create(LoanInput input)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = input.Type,
            Provider = Trim(input.Provider),
            AccountNumber = Trim(input.AccountNumber),
            BorrowerName = Trim(input.BorrowerName),
            OriginalAmount = input.OriginalAmount,
            OutstandingAmount = input.OutstandingAmount,
            InterestRate = input.InterestRate,
            EmiAmount = input.EmiAmount,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            NextDueDate = input.NextDueDate,
            Notes = Trim(input.Notes),
            Status = input.Status,
            InterestType = Trim(input.InterestType),
            ProcessingFee = input.ProcessingFee,
            CoApplicant = Trim(input.CoApplicant),
            CollateralType = Trim(input.CollateralType),
            AutoDebitEnabled = input.AutoDebitEnabled,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        using RichieDbContext db = _factory.Create();
        db.Loans.Add(loan);
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(Loan), loan.Id,
            $"Added {LoanTypeNames.Display(loan.Type)} of {loan.OutstandingAmount:N0} outstanding.");
        db.SaveChanges();
        return loan.Id;
    }

    public bool Update(Guid id, LoanInput input)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Loan? l = db.Loans.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (l is null) return false;
        l.Type = input.Type;
        l.Provider = Trim(input.Provider);
        l.AccountNumber = Trim(input.AccountNumber);
        l.BorrowerName = Trim(input.BorrowerName);
        l.OriginalAmount = input.OriginalAmount;
        l.OutstandingAmount = input.OutstandingAmount;
        l.InterestRate = input.InterestRate;
        l.EmiAmount = input.EmiAmount;
        l.StartDate = input.StartDate;
        l.EndDate = input.EndDate;
        l.NextDueDate = input.NextDueDate;
        l.Notes = Trim(input.Notes);
        l.Status = input.Status;
        l.InterestType = Trim(input.InterestType);
        l.ProcessingFee = input.ProcessingFee;
        l.CoApplicant = Trim(input.CoApplicant);
        l.CollateralType = Trim(input.CollateralType);
        l.AutoDebitEnabled = input.AutoDebitEnabled;
        l.UpdatedUtc = _clock.UtcNow;
        AuditWriter.Add(db, userId, l.UpdatedUtc, Module, AuditAction.Update, nameof(Loan), l.Id,
            $"Updated {LoanTypeNames.Display(l.Type)}.");
        db.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Loan? l = db.Loans.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (l is null) return false;
        db.Loans.Remove(l);
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(Loan), l.Id,
            $"Deleted {LoanTypeNames.Display(l.Type)}.");
        db.SaveChanges();
        return true;
    }

    public bool Close(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        Loan? l = db.Loans.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (l is null) return false;
        l.Status = LoanStatus.Closed;
        l.OutstandingAmount = 0;
        l.UpdatedUtc = _clock.UtcNow;
        AuditWriter.Add(db, userId, l.UpdatedUtc, Module, AuditAction.Update, nameof(Loan), l.Id,
            $"Closed {LoanTypeNames.Display(l.Type)}.");
        db.SaveChanges();
        return true;
    }

    public IReadOnlyList<LoanPaymentRow> GetPayments(Guid loanId)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.LoanPayments.AsNoTracking()
            .Where(p => p.LoanId == loanId && p.UserId == userId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new LoanPaymentRow(p.Id, p.PaymentType, p.Amount, p.PaymentDate, p.Note))
            .ToList();
    }

    public Guid RecordEmi(Guid loanId, decimal amount, decimal principalComponent,
        decimal interestComponent, DateTime paymentDate, string? note)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        Loan? l = db.Loans.FirstOrDefault(x => x.Id == loanId && x.UserId == userId);
        if (l is null) throw new InvalidOperationException("Loan not found.");
        var payment = new LoanPayment
        {
            Id = Guid.NewGuid(),
            LoanId = loanId,
            UserId = userId,
            PaymentType = LoanPaymentType.Emi,
            Amount = amount,
            PrincipalComponent = principalComponent,
            InterestComponent = interestComponent,
            PaymentDate = paymentDate,
            Note = Trim(note),
            CreatedUtc = now
        };
        db.LoanPayments.Add(payment);
        l.OutstandingAmount = Math.Max(0, l.OutstandingAmount - principalComponent);
        l.UpdatedUtc = now;
        if (l.OutstandingAmount == 0) l.Status = LoanStatus.Closed;
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(LoanPayment), payment.Id,
            $"Recorded EMI of {amount:N0} on {LoanTypeNames.Display(l.Type)}.");
        db.SaveChanges();
        return payment.Id;
    }

    public Guid RecordPrepayment(Guid loanId, decimal amount, DateTime paymentDate, string? note)
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        Loan? l = db.Loans.FirstOrDefault(x => x.Id == loanId && x.UserId == userId);
        if (l is null) throw new InvalidOperationException("Loan not found.");
        var payment = new LoanPayment
        {
            Id = Guid.NewGuid(),
            LoanId = loanId,
            UserId = userId,
            PaymentType = LoanPaymentType.Prepayment,
            Amount = amount,
            PrincipalComponent = amount,
            InterestComponent = 0,
            PaymentDate = paymentDate,
            Note = Trim(note),
            CreatedUtc = now
        };
        db.LoanPayments.Add(payment);
        l.OutstandingAmount = Math.Max(0, l.OutstandingAmount - amount);
        l.PrepaymentTotal += amount;
        l.UpdatedUtc = now;
        if (l.OutstandingAmount == 0) l.Status = LoanStatus.Closed;
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(LoanPayment), payment.Id,
            $"Recorded prepayment of {amount:N0} on {LoanTypeNames.Display(l.Type)}.");
        db.SaveChanges();
        return payment.Id;
    }

    public LiabilitiesSummary GetSummary()
    {
        Guid userId = UserId;
        DateTime now = _clock.UtcNow;
        using RichieDbContext db = _factory.Create();
        List<Loan> active = db.Loans.AsNoTracking()
            .Where(l => l.UserId == userId && l.Status != LoanStatus.Closed)
            .ToList();
        decimal totalOutstanding = active.Sum(l => l.OutstandingAmount);
        decimal totalEmi = active.Sum(l => l.EmiAmount);
        decimal totalInterest = active.Sum(l => TotalInterestPayable(l, now));
        var byType = active
            .GroupBy(l => l.Type)
            .Select(g => new LoanTypeSlice(
                g.Key, LoanTypeNames.Display(g.Key), g.Sum(l => l.OutstandingAmount),
                totalOutstanding > 0 ? Math.Round(g.Sum(l => l.OutstandingAmount) / totalOutstanding * 100, 1) : 0m,
                g.Count()))
            .OrderByDescending(s => s.Outstanding)
            .ToList();
        decimal monthlyIncome = EstimateMonthlyIncome(db, userId);
        decimal dti = monthlyIncome > 0 ? Math.Round(totalEmi / monthlyIncome * 100, 1) : 0m;
        (int score, string rating) = ComputeLoanHealthScore(active, monthlyIncome, totalOutstanding);
        return new LiabilitiesSummary(active.Count, totalOutstanding, totalEmi, totalInterest, dti, score, rating, byType);
    }

    public DebtHealthReport GetDebtHealth()
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        List<Loan> active = db.Loans.AsNoTracking()
            .Where(l => l.UserId == userId && l.Status != LoanStatus.Closed)
            .ToList();
        decimal monthlyIncome = EstimateMonthlyIncome(db, userId);
        decimal annualIncome = monthlyIncome * 12;
        decimal totalOutstanding = active.Sum(l => l.OutstandingAmount);
        decimal totalEmi = active.Sum(l => l.EmiAmount);
        decimal totalAssets = _assets.GetPortfolioSummary().TotalCurrentValue;
        decimal emiRatio = monthlyIncome > 0 ? Math.Round(totalEmi / monthlyIncome * 100, 1) : 0m;
        decimal dtiRatio = annualIncome > 0 ? Math.Round(totalOutstanding / annualIncome * 100, 1) : 0m;
        decimal dtaRatio = totalAssets > 0 ? Math.Round(totalOutstanding / totalAssets * 100, 1) : 0m;
        var rows = new List<DebtHealthRow>
        {
            new("EMI-to-Income", emiRatio, Band(emiRatio, 30m, 45m),
                "Share of monthly income going to EMIs (keep under 30%)."),
            new("Debt-to-Income", dtiRatio, Band(dtiRatio, 200m, 350m),
                "Total outstanding debt vs annual income."),
            new("Debt-to-Asset", dtaRatio, Band(dtaRatio, 50m, 100m),
                "How much of your assets are offset by debt.")
        };
        var recs = new List<string>();
        if (active.Any(l => l.InterestRate > HighInterestThresholdPercent))
            recs.Add("Prioritise repaying high-interest debt (e.g. credit cards, personal loans) first.");
        if (emiRatio > 45)
            recs.Add("Your EMI burden is high — consider refinancing or extending tenure to ease cash flow.");
        else if (emiRatio > 30)
            recs.Add("EMIs are moderate — avoid taking on new debt and consider small prepayments.");
        if (dtaRatio > 100)
            recs.Add("Liabilities exceed assets — focus on debt reduction before new investments.");
        if (recs.Count == 0)
            recs.Add("Your debt levels look healthy — keep paying EMIs on time and prepay when you can.");
        return new DebtHealthReport(rows, recs.Take(4).ToList());
    }

    public NetWorthSummary GetNetWorth()
    {
        Guid userId = UserId;
        decimal totalAssets = _assets.GetPortfolioSummary().TotalCurrentValue;
        using RichieDbContext db = _factory.Create();
        decimal totalLiabilities = db.Loans.AsNoTracking()
            .Where(l => l.UserId == userId && l.Status != LoanStatus.Closed)
            .Sum(l => (decimal?)l.OutstandingAmount) ?? 0m;
        return new NetWorthSummary(totalAssets, totalLiabilities, totalAssets - totalLiabilities);
    }

    private static string Band(decimal value, decimal safeMax, decimal moderateMax) =>
        value <= safeMax ? "Safe" : value <= moderateMax ? "Moderate" : "High";

    private static decimal EstimateMonthlyIncome(RichieDbContext db, Guid userId)
    {
        DateTime since = DateTime.UtcNow.AddMonths(-3);
        decimal total = db.Incomes.AsNoTracking()
            .Where(i => i.UserId == userId && i.Date >= since)
            .Sum(i => (decimal?)i.Amount) ?? 0m;
        return total > 0 ? Math.Round(total / 3, 2) : 0m;
    }

    private static int RemainingMonths(Loan l, DateTime now)
    {
        if (l.EndDate is DateTime end)
            return Math.Max(((end.Year - now.Year) * 12) + (end.Month - now.Month), 0);
        if (l.EmiAmount > 0)
            return Math.Max((int)Math.Round(l.OutstandingAmount / l.EmiAmount), 0);
        return 0;
    }

    private static decimal TotalInterestPayable(Loan l, DateTime now)
    {
        int months = RemainingMonths(l, now);
        if (l.EmiAmount <= 0 || months == 0) return 0;
        return Math.Max((l.EmiAmount * months) - l.OutstandingAmount, 0);
    }

    private static string EffectiveStatusName(Loan l, DateTime now)
    {
        if (l.Status == LoanStatus.Closed || l.OutstandingAmount <= 0) return "Closed";
        if (l.Status == LoanStatus.Defaulted) return "Defaulted";
        if (l.NextDueDate is DateTime due && due < now) return "Overdue";
        return "Active";
    }

    private (int Score, string Rating) ComputeLoanHealthScore(List<Loan> active,
        decimal monthlyIncome, decimal totalOutstanding)
    {
        decimal totalEmi = active.Sum(l => l.EmiAmount);
        decimal totalAssets = _assets.GetPortfolioSummary().TotalCurrentValue;
        decimal emiToIncome = monthlyIncome > 0
            ? Math.Clamp(100m - Math.Max(0m, totalEmi / monthlyIncome - 0.30m) * 250m, 0m, 100m)
            : 60m;
        decimal debtToAsset = totalAssets > 0
            ? Math.Clamp(100m - Math.Max(0m, totalOutstanding / totalAssets - 0.50m) * 100m, 0m, 100m)
            : totalOutstanding == 0 ? 60m : 40m;
        decimal highInterestOutstanding = active
            .Where(l => l.InterestRate > HighInterestThresholdPercent)
            .Sum(l => l.OutstandingAmount);
        decimal highInterestFactor = totalOutstanding > 0
            ? Math.Round(100m * (1 - highInterestOutstanding / totalOutstanding))
            : 100m;
        int score = Math.Clamp(
            (int)Math.Round(emiToIncome * 0.4m + debtToAsset * 0.3m + highInterestFactor * 0.3m), 0, 100);
        string rating = score >= 75 ? "Healthy" : score >= 50 ? "Manageable" : score >= 30 ? "Stretched" : "High Risk";
        return (score, rating);
    }

    private static LoanSummary ToSummary(Loan l, DateTime now) => new(
        l.Id, l.Type, LoanTypeNames.Display(l.Type), l.Provider, l.BorrowerName,
        l.OutstandingAmount, l.EmiAmount, l.InterestRate, l.NextDueDate,
        l.Status, EffectiveStatusName(l, now), RemainingMonths(l, now), TotalInterestPayable(l, now));

    private static LoanInput ToInput(Loan l) => new(
        l.Type, l.Provider, l.AccountNumber, l.BorrowerName, l.OriginalAmount, l.OutstandingAmount,
        l.InterestRate, l.EmiAmount, l.StartDate, l.EndDate, l.NextDueDate, l.Notes, l.Status,
        l.InterestType, l.ProcessingFee, l.CoApplicant, l.CollateralType, l.AutoDebitEnabled);

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
using Richie.Domain.Liabilities;

namespace Richie.Application.Liabilities;

public sealed record LoanInput(
    LoanType Type,
    string? Provider,
    string? AccountNumber,
    string? BorrowerName,
    decimal OriginalAmount,
    decimal OutstandingAmount,
    decimal InterestRate,
    decimal EmiAmount,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime? NextDueDate,
    string? Notes,
    LoanStatus Status,
    string? InterestType,
    decimal ProcessingFee,
    string? CoApplicant,
    string? CollateralType,
    bool AutoDebitEnabled);

public sealed record LoanSummary(
    Guid Id,
    LoanType Type,
    string TypeName,
    string? Provider,
    string? BorrowerName,
    decimal OutstandingAmount,
    decimal EmiAmount,
    decimal InterestRate,
    DateTime? NextDueDate,
    LoanStatus Status,
    string StatusName,
    int RemainingMonths,
    decimal TotalInterestPayable);

public sealed record LoanPaymentRow(
    Guid Id,
    LoanPaymentType PaymentType,
    decimal Amount,
    DateTime PaymentDate,
    string? Note);

public sealed record LoanTypeSlice(
    LoanType Type,
    string TypeName,
    decimal Outstanding,
    decimal Percent,
    int Count);

public sealed record LiabilitiesSummary(
    int ActiveLoanCount,
    decimal TotalOutstanding,
    decimal TotalMonthlyEmi,
    decimal TotalInterestPayable,
    decimal DebtToIncomePercent,
    int LoanHealthScore,
    string LoanHealthRating,
    IReadOnlyList<LoanTypeSlice> Distribution);

public sealed record DebtHealthRow(
    string Label,
    decimal ValuePercent,
    string Band,
    string Hint);

public sealed record DebtHealthReport(
    IReadOnlyList<DebtHealthRow> Rows,
    IReadOnlyList<string> Recommendations);

public sealed record NetWorthSummary(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal NetWorth);

public interface ILoanService
{
    IReadOnlyList<LoanSummary> GetLoans();
    LoanInput? GetById(Guid id);
    Guid Create(LoanInput input);
    bool Update(Guid id, LoanInput input);
    bool Delete(Guid id);
    bool Close(Guid id);
    IReadOnlyList<LoanPaymentRow> GetPayments(Guid loanId);
    Guid RecordEmi(Guid loanId, decimal amount, decimal principalComponent,
        decimal interestComponent, DateTime paymentDate, string? note);
    Guid RecordPrepayment(Guid loanId, decimal amount, DateTime paymentDate, string? note);
    LiabilitiesSummary GetSummary();
    DebtHealthReport GetDebtHealth();
    NetWorthSummary GetNetWorth();
}
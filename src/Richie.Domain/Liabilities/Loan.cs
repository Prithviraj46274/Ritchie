namespace Richie.Domain.Liabilities;

public enum LoanType
{
    Home = 1,
    Education = 2,
    Vehicle = 3,
    Personal = 4,
    CreditCard = 5,
    Gold = 6,
    Business = 7,
    Other = 8
}

public enum LoanStatus
{
    Active = 1,
    Closed = 2,
    Defaulted = 3
}

public enum LoanPaymentType
{
    Emi = 1,
    Prepayment = 2
}

public class Loan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public LoanType Type { get; set; }
    public string? Provider { get; set; }
    public string? AccountNumber { get; set; }
    public string? BorrowerName { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal EmiAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public decimal PrepaymentTotal { get; set; }
    public string? Notes { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public string? InterestType { get; set; }
    public decimal ProcessingFee { get; set; }
    public string? CoApplicant { get; set; }
    public string? CollateralType { get; set; }
    public bool AutoDebitEnabled { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
}

public class LoanPayment
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid UserId { get; set; }
    public LoanPaymentType PaymentType { get; set; } = LoanPaymentType.Emi;
    public decimal Amount { get; set; }
    public decimal PrincipalComponent { get; set; }
    public decimal InterestComponent { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedUtc { get; set; }
    public Loan Loan { get; set; } = null!;
}
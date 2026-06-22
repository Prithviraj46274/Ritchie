using Richie.Domain.Liabilities;

namespace Richie.Application.Liabilities;

public static class LoanTypeNames
{
    public static string Display(LoanType type) => type switch
    {
        LoanType.Home => "Home Loan",
        LoanType.Education => "Education Loan",
        LoanType.Vehicle => "Vehicle Loan",
        LoanType.Personal => "Personal Loan",
        LoanType.CreditCard => "Credit Card Debt",
        LoanType.Gold => "Gold Loan",
        LoanType.Business => "Business Loan",
        LoanType.Other => "Other Loan",
        _ => type.ToString()
    };

    public static string Display(LoanStatus status) => status switch
    {
        LoanStatus.Active => "Active",
        LoanStatus.Closed => "Closed",
        LoanStatus.Defaulted => "Defaulted",
        _ => status.ToString()
    };
}
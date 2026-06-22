using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Liabilities;

namespace Richie.UI.ViewModels;

public partial class LoanDetailsViewModel : ObservableObject
{
    private readonly ILoanService _loans;
    private Guid _loanId;

    [ObservableProperty] private string _loanTitle = string.Empty;
    [ObservableProperty] private string _statusName = string.Empty;
    [ObservableProperty] private decimal _outstandingAmount;
    [ObservableProperty] private decimal _emiAmount;
    [ObservableProperty] private decimal _interestRate;
    [ObservableProperty] private int _remainingMonths;
    [ObservableProperty] private decimal _totalInterestPayable;
    [ObservableProperty] private ObservableCollection<LoanPaymentRow> _payments = [];
    [ObservableProperty] private bool _hasNoPayments;
    [ObservableProperty] private string _paymentAmount = string.Empty;
    [ObservableProperty] private string _principalComponent = string.Empty;
    [ObservableProperty] private string _interestComponent = string.Empty;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string? _paymentNote;
    [ObservableProperty] private string? _error;

    public LoanDetailsViewModel(ILoanService loans) => _loans = loans;

    public void Initialize(Guid loanId)
    {
        _loanId = loanId;
        Refresh();
    }

    public void Refresh()
    {
        LoanInput? loan = _loans.GetById(_loanId);
        if (loan is null) return;

        LoanSummary? summary = _loans.GetLoans().FirstOrDefault(l => l.Id == _loanId);
        LoanTitle = $"{LoanTypeNames.Display(loan.Type)} — {loan.Provider ?? loan.BorrowerName ?? "Loan"}";
        StatusName = summary?.StatusName ?? LoanTypeNames.Display(loan.Status);
        OutstandingAmount = loan.OutstandingAmount;
        EmiAmount = loan.EmiAmount;
        InterestRate = loan.InterestRate;
        RemainingMonths = summary?.RemainingMonths ?? 0;
        TotalInterestPayable = summary?.TotalInterestPayable ?? 0;

        Payments = new ObservableCollection<LoanPaymentRow>(_loans.GetPayments(_loanId));
        HasNoPayments = Payments.Count == 0;
    }

    [RelayCommand]
    private void RecordEmi()
    {
        Error = null;
        if (!TryMoney(PaymentAmount, out decimal amount))
        { Error = "Enter a valid EMI amount."; return; }
        TryMoney(PrincipalComponent, out decimal principal);
        TryMoney(InterestComponent, out decimal interest);
        if (principal == 0 && interest == 0) principal = amount;

        _loans.RecordEmi(_loanId, amount, principal, interest, PaymentDate, PaymentNote);
        ClearPaymentForm();
        Refresh();
    }

    [RelayCommand]
    private void RecordPrepayment()
    {
        Error = null;
        if (!TryMoney(PaymentAmount, out decimal amount))
        { Error = "Enter a valid prepayment amount."; return; }

        _loans.RecordPrepayment(_loanId, amount, PaymentDate, PaymentNote);
        ClearPaymentForm();
        Refresh();
    }

    [RelayCommand]
    private void CloseLoan()
    {
        _loans.Close(_loanId);
        Refresh();
    }

    private void ClearPaymentForm()
    {
        PaymentAmount = string.Empty;
        PrincipalComponent = string.Empty;
        InterestComponent = string.Empty;
        PaymentNote = null;
        PaymentDate = DateTime.Today;
    }

    private static bool TryMoney(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
}
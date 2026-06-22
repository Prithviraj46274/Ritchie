using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Liabilities;
using Richie.Domain.Liabilities;

namespace Richie.UI.Views.Liabilities;

public partial class AddEditLoanViewModel : ObservableObject
{
    private readonly ILoanService _loans;
    private Guid? _editId;

    public sealed record TypeOption(LoanType Value, string Text);
    public sealed record StatusOption(LoanStatus Value, string Text);

    public IReadOnlyList<TypeOption> Types { get; } =
        Enum.GetValues<LoanType>().Select(t => new TypeOption(t, LoanTypeNames.Display(t))).ToList();

    public IReadOnlyList<StatusOption> Statuses { get; } =
        Enum.GetValues<LoanStatus>().Select(s => new StatusOption(s, LoanTypeNames.Display(s))).ToList();

    [ObservableProperty] private string _title = "Add loan";
    [ObservableProperty] private LoanType _type = LoanType.Home;
    [ObservableProperty] private LoanStatus _status = LoanStatus.Active;
    [ObservableProperty] private string? _provider;
    [ObservableProperty] private string? _accountNumber;
    [ObservableProperty] private string? _borrowerName;
    [ObservableProperty] private string _originalAmount = string.Empty;
    [ObservableProperty] private string _outstandingAmount = string.Empty;
    [ObservableProperty] private string _interestRate = string.Empty;
    [ObservableProperty] private string _emiAmount = string.Empty;
    [ObservableProperty] private DateTime? _startDate = DateTime.Today;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private DateTime? _nextDueDate = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _interestType;
    [ObservableProperty] private string _processingFee = "0";
    [ObservableProperty] private string? _coApplicant;
    [ObservableProperty] private string? _collateralType;
    [ObservableProperty] private bool _autoDebitEnabled;
    [ObservableProperty] private string? _error;

    public event Action<bool>? CloseRequested;

    public AddEditLoanViewModel(ILoanService loans) => _loans = loans;

    public void Initialize(Guid? id)
    {
        _editId = id;
        if (id is null) return;

        LoanInput? l = _loans.GetById(id.Value);
        if (l is null) return;

        Title = "Edit loan";
        Type = l.Type;
        Status = l.Status;
        Provider = l.Provider;
        AccountNumber = l.AccountNumber;
        BorrowerName = l.BorrowerName;
        OriginalAmount = l.OriginalAmount.ToString(CultureInfo.CurrentCulture);
        OutstandingAmount = l.OutstandingAmount.ToString(CultureInfo.CurrentCulture);
        InterestRate = l.InterestRate.ToString(CultureInfo.CurrentCulture);
        EmiAmount = l.EmiAmount.ToString(CultureInfo.CurrentCulture);
        StartDate = l.StartDate;
        EndDate = l.EndDate;
        NextDueDate = l.NextDueDate;
        Notes = l.Notes;
        InterestType = l.InterestType;
        ProcessingFee = l.ProcessingFee.ToString(CultureInfo.CurrentCulture);
        CoApplicant = l.CoApplicant;
        CollateralType = l.CollateralType;
        AutoDebitEnabled = l.AutoDebitEnabled;
    }

    [RelayCommand]
    private void Save()
    {
        Error = null;
        if (!TryMoney(OriginalAmount, out decimal original))
        { Error = "Enter a valid original loan amount."; return; }
        if (!TryMoney(OutstandingAmount, out decimal outstanding))
        { Error = "Enter a valid outstanding amount."; return; }
        if (!TryMoney(InterestRate, out decimal rate))
        { Error = "Enter a valid interest rate."; return; }
        if (!TryMoney(EmiAmount, out decimal emi))
        { Error = "Enter a valid EMI amount."; return; }
        if (!TryMoney(ProcessingFee, out decimal fee)) fee = 0;

        var input = new LoanInput(Type, Provider, AccountNumber, BorrowerName, original, outstanding,
            rate, emi, StartDate, EndDate, NextDueDate, Notes, Status, InterestType, fee,
            CoApplicant, CollateralType, AutoDebitEnabled);

        if (_editId is null) _loans.Create(input);
        else _loans.Update(_editId.Value, input);

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private static bool TryMoney(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
}
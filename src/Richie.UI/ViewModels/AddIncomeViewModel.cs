using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Income;

namespace Richie.UI.ViewModels;

public partial class AddIncomeViewModel : ObservableObject
{
    private readonly IIncomeService _income;
    private Guid? _editId;

    [ObservableProperty] private string _title = "Add income";
    [ObservableProperty] private DateTime? _date = DateTime.Today;
    [ObservableProperty] private string _amountText = string.Empty;
    [ObservableProperty] private string _source = string.Empty;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _error;

    public event Action<bool>? CloseRequested;

    public AddIncomeViewModel(IIncomeService income) => _income = income;

    public void Initialize(Guid? id)
    {
        _editId = id;
        if (id is null)
            return;

        IncomeInput? i = _income.GetById(id.Value);
        if (i is null)
            return;

        Title = "Edit income";
        Date = i.Date;
        AmountText = i.Amount.ToString(CultureInfo.CurrentCulture);
        Source = i.Source;
        Notes = i.Notes;
    }

    [RelayCommand]
    private void Save()
    {
        Error = null;
        if (Date is null)
        {
            Error = "Date is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Source))
        {
            Error = "Source is required (e.g. Salary).";
            return;
        }
        if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal amount) || amount <= 0)
        {
            Error = "Enter a valid amount.";
            return;
        }

        var input = new IncomeInput(Date.Value, amount, Source, Notes);
        if (_editId is null)
            _income.Create(input);
        else
            _income.Update(_editId.Value, input);

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}

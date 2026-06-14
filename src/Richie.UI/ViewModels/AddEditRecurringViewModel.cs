using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Expenses;
using Richie.Domain.Expenses;

namespace Richie.UI.ViewModels;

public partial class AddEditRecurringViewModel : ObservableObject
{
    private readonly IExpenseRecurringService _recurring;
    private Guid? _editId;

    public sealed record CategoryOption(ExpenseCategory Value, string Text);
    public sealed record FrequencyOption(ExpenseRecurringFrequency Value, string Text);

    public IReadOnlyList<CategoryOption> Categories { get; } =
        Enum.GetValues<ExpenseCategory>().Select(c => new CategoryOption(c, ExpenseCategoryNames.Display(c))).ToList();

    public IReadOnlyList<FrequencyOption> Frequencies { get; } =
        Enum.GetValues<ExpenseRecurringFrequency>().Select(f => new FrequencyOption(f, f.ToString())).ToList();

    [ObservableProperty] private string _title = "Add recurring expense";
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _amountText = string.Empty;
    [ObservableProperty] private ExpenseCategory _category = ExpenseCategory.HousingUtilities;
    [ObservableProperty] private string? _spentBy;
    [ObservableProperty] private string? _spentFor;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private ExpenseRecurringFrequency _frequency = ExpenseRecurringFrequency.Monthly;
    [ObservableProperty] private DateTime? _startDate = DateTime.Today;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private string? _error;

    public event Action<bool>? CloseRequested;

    public AddEditRecurringViewModel(IExpenseRecurringService recurring) => _recurring = recurring;

    public void Initialize(Guid? id)
    {
        _editId = id;
        if (id is null)
            return;

        RecurringInput? r = _recurring.GetRule(id.Value);
        if (r is null)
            return;

        Title = "Edit recurring expense";
        IsEnabled = r.IsEnabled;
        AmountText = r.Amount.ToString(CultureInfo.CurrentCulture);
        Category = r.Category;
        SpentBy = r.SpentBy;
        SpentFor = r.SpentFor;
        Notes = r.Notes;
        Frequency = r.Frequency;
        StartDate = r.StartDate;
        EndDate = r.EndDate;
    }

    [RelayCommand]
    private void Save()
    {
        Error = null;
        if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal amount) || amount <= 0)
        {
            Error = "Enter a valid amount.";
            return;
        }
        if (StartDate is null)
        {
            Error = "Start date is required.";
            return;
        }
        if (EndDate is { } end && end < StartDate)
        {
            Error = "End date cannot be before the start date.";
            return;
        }

        var input = new RecurringInput(IsEnabled, amount, Category, SpentBy, SpentFor, Notes, Frequency, StartDate.Value, EndDate);
        if (_editId is null)
            _recurring.CreateRule(input);
        else
            _recurring.UpdateRule(_editId.Value, input);

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}

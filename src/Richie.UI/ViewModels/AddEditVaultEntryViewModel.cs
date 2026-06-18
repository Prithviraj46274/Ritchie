using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Vault;

namespace Richie.UI.ViewModels;

public partial class AddEditVaultEntryViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private Guid? _editId;

    [ObservableProperty] private string _title = "Add password";
    [ObservableProperty] private string _accountName = string.Empty;
    [ObservableProperty] private string? _category;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private string? _loginId;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string _passwordHint = "Password";
    [ObservableProperty] private string? _error;

    [ObservableProperty] private bool _showStrength;
    [ObservableProperty] private string _strengthText = string.Empty;
    [ObservableProperty] private int _strengthPercent;
    [ObservableProperty] private Brush _strengthBrush = Brushes.Transparent;

    // Soft status palette (green good / amber attention / red critical) — consistent app-wide.
    private static bool IsDarkMode => Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme() == Wpf.Ui.Appearance.ApplicationTheme.Dark;

    private static Brush Red => IsDarkMode
        ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
        : new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C));

    private static Brush Amber => IsDarkMode
        ? new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B))
        : new SolidColorBrush(Color.FromRgb(0x9D, 0x5D, 0x00));

    private static Brush Green => IsDarkMode
        ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
        : new SolidColorBrush(Color.FromRgb(0x0F, 0x7B, 0x0F));

    public event Action<bool>? CloseRequested;

    public AddEditVaultEntryViewModel(IVaultService vault) => _vault = vault;

    public void Initialize(Guid? id)
    {
        _editId = id;
        if (id is null)
            return;

        VaultEntryDetail? e = _vault.GetById(id.Value);
        if (e is null)
            return;

        Title = "Edit password";
        AccountName = e.AccountName;
        Category = e.Category;
        Url = e.Url;
        LoginId = e.LoginId;
        Notes = e.Notes;
        PasswordHint = "Password (leave blank to keep current)";
    }

    [RelayCommand]
    private void Save()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(AccountName))
        {
            Error = "Account name is required.";
            return;
        }
        if (_editId is null && string.IsNullOrWhiteSpace(Password))
        {
            Error = "A password is required.";
            return;
        }

        var input = new VaultEntryInput(AccountName, Category, Url, LoginId, Password, Notes);
        if (_editId is null)
            _vault.Create(input);
        else
            _vault.Update(_editId.Value, input);

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    partial void OnPasswordChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            ShowStrength = false;
            return;
        }

        PasswordStrengthResult s = PasswordStrength.Evaluate(value);
        StrengthText = $"Strength: {s.Label}";
        StrengthPercent = s.Percent;
        StrengthBrush = s.Score <= 1 ? Red : s.Score == 2 ? Amber : Green;
        ShowStrength = true;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Vault;

namespace Richie.UI.ViewModels;

/// <summary>
/// Read-only credential details (PRD §8.5). The password stays masked until the user passes a
/// re-auth (handled by the window) and calls <see cref="Reveal"/>; <see cref="GetPassword"/>
/// fetches the plaintext for copy-to-clipboard.
/// </summary>
public partial class VaultEntryDetailsViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private Guid _id;

    [ObservableProperty] private string _accountName = string.Empty;
    [ObservableProperty] private string? _category;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private string? _loginId;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private bool _isRevealed;
    [ObservableProperty] private string _displayPassword = Masked;

    private const string Masked = "••••••••";

    public bool HasUrl => !string.IsNullOrWhiteSpace(Url);

    public string RevealButtonText => IsRevealed ? "Hide" : "Reveal";

    public event Action? CloseRequested;

    public VaultEntryDetailsViewModel(IVaultService vault) => _vault = vault;

    public void Initialize(Guid id)
    {
        VaultEntryDetail? e = _vault.GetById(id);
        if (e is null)
            return;

        _id = id;
        AccountName = e.AccountName;
        Category = e.Category;
        Url = e.Url;
        LoginId = e.LoginId;
        Notes = e.Notes;
        Hide();
        OnPropertyChanged(nameof(HasUrl));
    }

    /// <summary>Plaintext for clipboard copy — caller must have re-authenticated first.</summary>
    public string? GetPassword() => _vault.RevealPassword(_id);

    public void Reveal()
    {
        string? plaintext = GetPassword();
        if (plaintext is null)
            return;
        DisplayPassword = plaintext;
        IsRevealed = true;
    }

    public void Hide()
    {
        DisplayPassword = Masked;
        IsRevealed = false;
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    partial void OnIsRevealedChanged(bool value) => OnPropertyChanged(nameof(RevealButtonText));
}

using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Vault;
using Wpf.Ui.Controls;

namespace Richie.UI.ViewModels;

/// <summary>One vault grid row. Holds per-row reveal state and the decrypted password while shown;
/// the password is fetched on demand (after re-auth) and dropped again on hide.</summary>
public partial class VaultEntryRowViewModel : ObservableObject
{
    private const string Masked = "••••••••";

    public Guid Id { get; }
    public string AccountName { get; }
    public string? Category { get; }
    public string? Url { get; }
    public string? LoginId { get; }

    [ObservableProperty] private string _displayPassword = Masked;
    [ObservableProperty] private bool _isRevealed;

    public VaultEntryRowViewModel(VaultEntrySummary summary)
    {
        Id = summary.Id;
        AccountName = summary.AccountName;
        Category = summary.Category;
        Url = summary.Url;
        LoginId = summary.LoginId;
    }

    public SymbolRegular EyeSymbol => IsRevealed ? SymbolRegular.EyeOff24 : SymbolRegular.Eye24;
    public string EyeTooltip => IsRevealed ? "Hide password" : "Reveal password";

    public void Reveal(string plaintext)
    {
        DisplayPassword = plaintext;
        IsRevealed = true;
    }

    public void Hide()
    {
        DisplayPassword = Masked;
        IsRevealed = false;
    }

    partial void OnIsRevealedChanged(bool value)
    {
        OnPropertyChanged(nameof(EyeSymbol));
        OnPropertyChanged(nameof(EyeTooltip));
    }
}

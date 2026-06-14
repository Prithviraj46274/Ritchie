using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Vault;

namespace Richie.UI.ViewModels;

/// <summary>Modal re-authentication prompt — confirms the master password before a reveal/copy
/// (PRD §8.5). Verifies via <see cref="IVaultGate.Verify"/> without changing lock state.</summary>
public partial class VaultReauthViewModel : ObservableObject
{
    private readonly IVaultGate _gate;

    [ObservableProperty] private string _prompt = "Re-enter your master password to continue.";
    [ObservableProperty] private string _masterPassword = string.Empty;
    [ObservableProperty] private string? _error;

    public event Action<bool>? CloseRequested;

    public VaultReauthViewModel(IVaultGate gate) => _gate = gate;

    public void Configure(string prompt) => Prompt = prompt;

    [RelayCommand]
    private void Confirm()
    {
        Error = null;
        if (_gate.Verify(MasterPassword))
        {
            MasterPassword = string.Empty;
            CloseRequested?.Invoke(true);
            return;
        }
        Error = "Incorrect master password.";
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}

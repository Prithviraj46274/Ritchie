using System.Windows.Input;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Vault;

public partial class VaultReauthWindow : FluentWindow
{
    private readonly VaultReauthViewModel _vm;

    public VaultReauthWindow(VaultReauthViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.CloseRequested += OnCloseRequested;
        Closed += (_, _) => vm.CloseRequested -= OnCloseRequested;
        Loaded += (_, _) => PasswordBox.Focus();
    }

    public void Configure(string prompt, bool unlockOnConfirm = false) => _vm.Configure(prompt, unlockOnConfirm);

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.ConfirmCommand.Execute(null);
    }

    private void OnCloseRequested(bool success)
    {
        DialogResult = success;
        Close();
    }
}

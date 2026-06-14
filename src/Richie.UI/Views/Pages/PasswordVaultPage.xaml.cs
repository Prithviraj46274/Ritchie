using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using Richie.UI.Views.Vault;

namespace Richie.UI.Views.Pages;

public partial class PasswordVaultPage : Page
{
    public PasswordVaultPage()
    {
        InitializeComponent();
        DataContext = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<PasswordVaultViewModel>();
    }

    private PasswordVaultViewModel Vm => (PasswordVaultViewModel)DataContext;

    // Re-lock on every access to the page (PRD §8.1).
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Vm.ResetToLocked();
        MasterPasswordBox.Focus();
    }

    private void OnMasterPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Vm.Submit();
    }

    private void OnSubmit(object sender, RoutedEventArgs e) => Vm.Submit();

    private void OnLock(object sender, RoutedEventArgs e)
    {
        Vm.ResetToLocked();
        MasterPasswordBox.Focus();
    }

    private void OnAddEntry(object sender, RoutedEventArgs e) => OpenEditor(null);

    private void OnViewEntry(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<VaultEntryDetailsWindow>();
        window.Owner = Window.GetWindow(this);
        window.Details.Initialize(id);
        window.ShowDialog();
    }

    private void OnEditEntry(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
            OpenEditor(id);
    }

    private void OnDeleteEntry(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        if (MessageBox.Show("Delete this credential?", "Confirm delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            Vm.Delete(id);
    }

    private void OpenEditor(Guid? id)
    {
        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<AddEditVaultEntryWindow>();
        window.Owner = Window.GetWindow(this);
        window.Editor.Initialize(id);
        if (window.ShowDialog() == true)
            Vm.Reload();
    }
}

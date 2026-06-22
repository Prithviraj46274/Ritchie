using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Liabilities;

public partial class LiabilitiesWindow : FluentWindow
{
    private readonly LiabilitiesViewModel _vm;

    public LiabilitiesWindow(LiabilitiesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void OnAddLoan(object sender, RoutedEventArgs e) => OpenEditor(null);

    private void OnEditLoan(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
            OpenEditor(id);
    }

    private void OnViewDetails(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id }) return;
        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<LoanDetailsWindow>();
        window.Owner = this;
        window.Initialize(id);
        window.ShowDialog();
        _vm.Refresh();
    }

    private void OnDeleteLoan(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id }) return;
        if (System.Windows.MessageBox.Show(
                "Delete this loan? This also removes its payment history.",
                "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            == System.Windows.MessageBoxResult.Yes)
            _vm.Delete(id);
    }

    private void OpenEditor(Guid? id)
    {
        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<AddEditLoanWindow>();
        window.Owner = this;
        window.Editor.Initialize(id);
        if (window.ShowDialog() == true)
            _vm.Refresh();
    }
}
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using Richie.UI.Views.Liabilities;

namespace Richie.UI.Views.Pages;

public partial class LiabilitiesPage : Page
{
    private LiabilitiesViewModel Vm => (LiabilitiesViewModel)DataContext;

    public LiabilitiesPage()
    {
        InitializeComponent();
        DataContext = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<LiabilitiesViewModel>();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Vm.Refresh();

    private void OnAddLoan(object sender, RoutedEventArgs e) => OpenEditor(null);

    private void OnEditLoan(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
            OpenEditor(id);
    }

    private void OnDeleteLoan(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id }) return;
        if (System.Windows.MessageBox.Show(
                "Delete this loan?", "Confirm delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning)
            == System.Windows.MessageBoxResult.Yes)
        {
            Vm.Delete(id);
        }
    }

    private void OpenEditor(Guid? id)
    {
        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<AddEditLoanWindow>();
        window.Owner = Window.GetWindow(this);
        window.Editor.Initialize(id);
        if (window.ShowDialog() == true)
            Vm.Refresh();
    }
}
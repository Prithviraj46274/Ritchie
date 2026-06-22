using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using Richie.UI.Views.Insurance;

namespace Richie.UI.Views.Pages;

public partial class FinancialHealthAuditPage : Page
{
    public FinancialHealthAuditPage()
    {
        InitializeComponent();
        DataContext = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<HealthAuditViewModel>();
    }

    private HealthAuditViewModel Vm => (HealthAuditViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e) => Vm.Load();

    private void OnManageInsurance(object sender, RoutedEventArgs e)
    {
        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<InsuranceWindow>();
        window.Owner = Window.GetWindow(this);
        window.ShowDialog();
        Vm.Load();
    }
}
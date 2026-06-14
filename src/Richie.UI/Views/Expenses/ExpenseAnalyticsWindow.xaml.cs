using System.Windows;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Expenses;

public partial class ExpenseAnalyticsWindow : FluentWindow
{
    private readonly ExpenseAnalyticsViewModel _vm;

    public ExpenseAnalyticsWindow(ExpenseAnalyticsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void OnSaveBudgets(object sender, RoutedEventArgs e) => _vm.SaveBudgets();
}

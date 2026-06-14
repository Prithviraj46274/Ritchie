using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Expenses;

public partial class RecurringExpensesWindow : FluentWindow
{
    private readonly RecurringExpensesViewModel _vm;

    public RecurringExpensesWindow(RecurringExpensesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void OnAdd(object sender, RoutedEventArgs e) => OpenEditor(null);

    private void OnEdit(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
            OpenEditor(id);
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        if (System.Windows.MessageBox.Show("Delete this recurring expense?", "Confirm delete",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
            == System.Windows.MessageBoxResult.Yes)
            _vm.Delete(id);
    }

    private void OpenEditor(Guid? id)
    {
        var window = ((App)System.Windows.Application.Current).Services.GetRequiredService<AddEditRecurringWindow>();
        window.Owner = this;
        window.Editor.Initialize(id);
        if (window.ShowDialog() == true)
            _vm.Refresh();
    }
}

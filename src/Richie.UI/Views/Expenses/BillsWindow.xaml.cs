using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Richie.UI.Services;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Expenses;

public partial class BillsWindow : FluentWindow
{
    public BillsViewModel Bills { get; }

    public BillsWindow(BillsViewModel vm)
    {
        InitializeComponent();
        Bills = vm;
        DataContext = vm;
    }

    private void OnAttach(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Attach a bill or receipt",
            Filter = "Documents and images|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*"
        };
        if (dialog.ShowDialog(this) == true)
        {
            Bills.Attach(dialog.FileName);
            Bills.Refresh();
            ((App)System.Windows.Application.Current).Services
                .GetRequiredService<ToastService>().Success("Bill attached.");
        }
    }

    private void OnDownload(object sender, RoutedEventArgs e)
    {
        if (Bills.SelectedDocument is not { } doc)
            return;

        var dialog = new SaveFileDialog { FileName = doc.FileName, Title = "Save a copy" };
        if (dialog.ShowDialog(this) == true)
            Bills.Download(dialog.FileName);
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (Bills.SelectedDocument is null)
            return;

        if (System.Windows.MessageBox.Show("Delete this bill? This cannot be undone.", "Confirm delete",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
            == System.Windows.MessageBoxResult.Yes)
            Bills.DeleteSelected();
    }

    private void OnPrevious(object sender, RoutedEventArgs e) => Bills.SelectPrevious();

    private void OnNext(object sender, RoutedEventArgs e) => Bills.SelectNext();
}

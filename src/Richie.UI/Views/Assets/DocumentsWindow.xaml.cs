using System.Windows;
using Microsoft.Win32;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Assets;

public partial class DocumentsWindow : FluentWindow
{
    private readonly DocumentsViewModel _vm;

    public DocumentsViewModel Documents => _vm;

    public DocumentsWindow(DocumentsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void OnAttach(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Attach a document",
            Filter = "Documents and images|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            _vm.Attach(dialog.FileName);
    }

    private void OnDownload(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedDocument is not { } doc)
            return;

        var dialog = new SaveFileDialog { FileName = doc.FileName, Title = "Save a copy" };
        if (dialog.ShowDialog(this) == true)
            _vm.Download(dialog.FileName);
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedDocument is null)
            return;

        if (System.Windows.MessageBox.Show("Delete this document? This cannot be undone.", "Confirm delete",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
            == System.Windows.MessageBoxResult.Yes)
            _vm.DeleteSelected();
    }

    private void OnPrevious(object sender, RoutedEventArgs e) => _vm.SelectPrevious();

    private void OnNext(object sender, RoutedEventArgs e) => _vm.SelectNext();
}

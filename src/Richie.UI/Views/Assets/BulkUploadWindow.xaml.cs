using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Richie.UI.Services;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Assets;

public partial class BulkUploadWindow : FluentWindow
{
    public BulkUploadViewModel Upload { get; }

    public BulkUploadWindow(BulkUploadViewModel vm)
    {
        InitializeComponent();
        Upload = vm;
        DataContext = vm;
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a file to import",
            Filter = "CSV or Excel|*.csv;*.xlsx"
        };
        if (dialog.ShowDialog(this) == true)
        {
            Upload.ImportFile(dialog.FileName);
            NotifyResult();
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            Upload.ImportFile(files[0]);
            NotifyResult();
        }
    }

    private void NotifyResult()
    {
        var toast = ((App)System.Windows.Application.Current).Services.GetRequiredService<ToastService>();
        if (Upload.ImportedAny)
            toast.Success(Upload.Summary, "Import complete");
        else
            toast.Info(Upload.Summary, "Import");
    }

    private void OnDownloadCsv(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "richie-assets-template.csv", Filter = "CSV|*.csv" };
        if (dialog.ShowDialog(this) == true)
            Upload.SaveCsvTemplate(dialog.FileName);
    }

    private void OnDownloadExcel(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "richie-assets-template.xlsx", Filter = "Excel|*.xlsx" };
        if (dialog.ShowDialog(this) == true)
            Upload.SaveExcelTemplate(dialog.FileName);
    }
}

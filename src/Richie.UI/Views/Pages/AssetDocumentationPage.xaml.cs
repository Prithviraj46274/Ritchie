using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using Richie.UI.Views.Assets;

namespace Richie.UI.Views.Pages;

public partial class AssetDocumentationPage : Page
{
    public AssetDocumentationPage()
    {
        InitializeComponent();
        DataContext = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<AssetDocumentationViewModel>();
    }

    private AssetDocumentationViewModel Vm => (AssetDocumentationViewModel)DataContext;

    private void OnAddAsset(object sender, RoutedEventArgs e) => OpenEditor(null);

    private void OnOpenGoals(object sender, RoutedEventArgs e)
    {
        var window = ((App)System.Windows.Application.Current).Services.GetRequiredService<GoalsWindow>();
        window.Owner = Window.GetWindow(this);
        window.ShowDialog();
        Vm.Refresh(); // goal links don't change assets, but current values may have been recalculated
    }

    private void OnViewAsset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        var window = ((App)System.Windows.Application.Current).Services.GetRequiredService<AssetDetailsWindow>();
        window.Owner = Window.GetWindow(this);
        window.Details.Initialize(id);
        window.ShowDialog();

        bool editRequested = window.Details.EditRequested;
        Vm.Refresh(); // exclusion may have changed
        if (editRequested)
            OpenEditor(id);
    }

    private void OnEditAsset(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Guid id })
            OpenEditor(id);
    }

    private void OnSipAsset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        string assetName = Vm.Items.FirstOrDefault(a => a.Id == id)?.Name ?? "Asset";
        var window = ((App)System.Windows.Application.Current).Services.GetRequiredService<SipScheduleWindow>();
        window.Owner = Window.GetWindow(this);
        window.Schedule.Initialize(id, assetName);
        window.ShowDialog();
        Vm.Refresh();
    }

    private void OnDocsAsset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        string assetName = Vm.Items.FirstOrDefault(a => a.Id == id)?.Name ?? "Asset";
        var window = ((App)System.Windows.Application.Current).Services.GetRequiredService<DocumentsWindow>();
        window.Owner = Window.GetWindow(this);
        window.Documents.Initialize(id, assetName);
        window.ShowDialog();
    }

    private void OnDeleteAsset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: Guid id })
            return;

        MessageBoxResult confirm = MessageBox.Show(
            "Delete this asset? This cannot be undone.", "Confirm delete",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
            Vm.Delete(id);
    }

    private void OpenEditor(Guid? assetId)
    {
        var window = ((App)System.Windows.Application.Current).Services.GetRequiredService<AddEditAssetWindow>();
        window.Owner = Window.GetWindow(this);
        window.Editor.Initialize(assetId);

        if (window.ShowDialog() == true)
            Vm.Refresh();
    }
}

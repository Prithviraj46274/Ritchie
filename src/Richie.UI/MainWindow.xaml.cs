using Richie.UI.Views.Pages;
using Wpf.Ui.Controls;

namespace Richie.UI;

/// <summary>
/// The application shell: a Fluent (Mica) window hosting the primary NavigationView.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RootNavigation.Navigate(typeof(DashboardPage));
    }
}

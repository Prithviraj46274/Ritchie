using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Richie.UI.Views.Pages;

public partial class HelpPage : Page
{
    public HelpPage()
    {
        InitializeComponent();
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionTextBlock.Text = $"Version {v?.ToString(3) ?? "1.0.0"}.";
    }

    private void OnReplayTour(object sender, RoutedEventArgs e) =>
        ((App)System.Windows.Application.Current).RequestTour();
}

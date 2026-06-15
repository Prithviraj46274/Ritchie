using System.Windows;
using Richie.Infrastructure;

namespace Richie.UI.Views;

public partial class FirstRunWindow : Window
{
    public FirstRunWindow()
    {
        InitializeComponent();
        DataPathText.Text = AppPaths.DataDirectory;
    }

    private void OnSetup(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}

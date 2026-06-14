using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Vault;

public partial class VaultEntryDetailsWindow : FluentWindow
{
    private readonly VaultEntryDetailsViewModel _vm;
    private DispatcherTimer? _clearTimer;
    private string? _copied;

    public VaultEntryDetailsViewModel Details => _vm;

    public VaultEntryDetailsWindow(VaultEntryDetailsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.CloseRequested += OnCloseRequested;
        Closed += (_, _) => vm.CloseRequested -= OnCloseRequested;
    }

    private void OnReveal(object sender, RoutedEventArgs e)
    {
        if (_vm.IsRevealed)
        {
            _vm.Hide();
            return;
        }
        if (Reauthenticate("Re-enter your master password to reveal this password."))
            _vm.Reveal();
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        // A copy exposes the secret, so require re-auth unless it's already revealed on screen.
        if (!_vm.IsRevealed && !Reauthenticate("Re-enter your master password to copy this password."))
            return;

        string? plaintext = _vm.GetPassword();
        if (string.IsNullOrEmpty(plaintext))
            return;

        CopyWithAutoClear(plaintext);
    }

    private void OnOpenWebsite(object sender, RoutedEventArgs e)
    {
        string? url = _vm.Url;
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            System.Windows.MessageBox.Show("This entry's URL is not a valid http(s) link.", "Open website",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Could not open the website: {ex.Message}", "Open website",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private bool Reauthenticate(string prompt)
    {
        var window = ((App)System.Windows.Application.Current).Services
            .GetRequiredService<VaultReauthWindow>();
        window.Owner = this;
        window.Configure(prompt);
        return window.ShowDialog() == true;
    }

    // Copies to clipboard and clears it after 30s (PRD §8.5), but only if it still holds our value.
    private void CopyWithAutoClear(string value)
    {
        System.Windows.Clipboard.SetText(value);
        _copied = value;

        _clearTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clearTimer.Stop();
        _clearTimer.Tick -= OnClearTick;
        _clearTimer.Tick += OnClearTick;
        _clearTimer.Start();
    }

    private void OnClearTick(object? sender, EventArgs e)
    {
        _clearTimer?.Stop();
        try
        {
            if (_copied is not null && System.Windows.Clipboard.GetText() == _copied)
                System.Windows.Clipboard.Clear();
        }
        catch
        {
            // Clipboard can be momentarily locked by another process — ignore.
        }
        _copied = null;
    }

    private void OnCloseRequested() => Close();
}

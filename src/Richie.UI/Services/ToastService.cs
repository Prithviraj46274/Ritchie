using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Richie.UI.Services;

/// <summary>App-wide toast notifications (WPF-UI Snackbar). The presenter is set once by MainWindow;
/// any view/VM resolves this from the container and calls Success/Info.</summary>
public sealed class ToastService
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    private readonly ISnackbarService _snackbar;

    public ToastService(ISnackbarService snackbar) => _snackbar = snackbar;

    public void Success(string message, string title = "Done") =>
        _snackbar.Show(title, message, ControlAppearance.Success, null, Duration);

    public void Info(string message, string title = "Richie") =>
        _snackbar.Show(title, message, ControlAppearance.Info, null, Duration);
}

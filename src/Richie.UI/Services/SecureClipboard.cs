using System.Windows;
using System.Windows.Threading;

namespace Richie.UI.Services;

/// <summary>Copies a secret to the clipboard and auto-clears it after a delay (PRD §8.5,
/// default 30s), but only if the clipboard still holds the copied value. The timer runs on the
/// UI dispatcher so it survives the originating window closing.</summary>
internal static class SecureClipboard
{
    private static DispatcherTimer? _timer;
    private static string? _copied;

    public static void CopyWithAutoClear(string value, int seconds = 30)
    {
        Clipboard.SetText(value);
        _copied = value;

        _timer ??= new DispatcherTimer();
        _timer.Stop();
        _timer.Interval = TimeSpan.FromSeconds(seconds);
        _timer.Tick -= OnTick;
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private static void OnTick(object? sender, EventArgs e)
    {
        _timer?.Stop();
        try
        {
            if (_copied is not null && Clipboard.GetText() == _copied)
                Clipboard.Clear();
        }
        catch
        {
            // Clipboard can be momentarily locked by another process — ignore.
        }
        _copied = null;
    }
}

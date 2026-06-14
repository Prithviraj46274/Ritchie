using System.Diagnostics;

namespace Richie.UI.Services;

/// <summary>Opens user-supplied web links in the default browser, restricted to http/https.</summary>
internal static class UrlLauncher
{
    /// <summary>Launches the URL if it is a valid absolute http(s) link. Returns false otherwise
    /// (missing, non-http(s), or launch failure).</summary>
    public static bool TryOpen(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }
}

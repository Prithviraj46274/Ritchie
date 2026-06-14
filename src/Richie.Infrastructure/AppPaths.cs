namespace Richie.Infrastructure;

/// <summary>
/// Canonical local file locations for the app, all under %LOCALAPPDATA%\Richie.
/// (The PRD's documents/photos/receipts/exports/backups layout is added in later phases.)
/// </summary>
public static class AppPaths
{
    // Settable so tests can redirect storage to a temp directory; production uses the default.
    public static string DataDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Richie");

    public static string LogsDirectory => Path.Combine(DataDirectory, "logs");
    public static string DatabasePath => Path.Combine(DataDirectory, "richie.db");
    public static string DatabaseKeyPath => Path.Combine(DataDirectory, "db.key");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}

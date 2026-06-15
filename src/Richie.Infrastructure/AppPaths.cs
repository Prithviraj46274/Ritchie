namespace Richie.Infrastructure;

/// <summary>
/// Canonical local file locations for the app, all under %LOCALAPPDATA%\Richie.
/// </summary>
public static class AppPaths
{
    // Settable so tests can redirect storage to a temp directory; production uses the default.
    public static string DataDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Richie");

    public static string LogsDirectory => Path.Combine(DataDirectory, "logs");
    public static string DatabasePath => Path.Combine(DataDirectory, "richie.db");
    public static string DatabaseKeyPath => Path.Combine(DataDirectory, "db.key");

    public static bool IsFirstRun => !File.Exists(DatabaseKeyPath);

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(Path.Combine(DataDirectory, "documents", "assets"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "photos", "assets"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "documents", "insurance"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "receipts", "expenses"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "exports"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "backups"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "templates"));
    }
}

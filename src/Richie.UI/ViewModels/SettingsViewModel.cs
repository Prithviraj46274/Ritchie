using System.Collections.ObjectModel;
using System.Windows.Media;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Assets;
using Richie.Application.Common;
using Richie.Application.Settings;
using Richie.Domain.Notifications;
using Richie.UI.Services;
using Wpf.Ui.Appearance;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Richie.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IAppSettingsService _settings;
    private readonly IAssetService _assets;
    private readonly InactivityLockService _lock;

    public partial class NotificationPref : ObservableObject
    {
        public NotificationType Type { get; init; }
        public string Name { get; init; } = string.Empty;
        [ObservableProperty] private bool _isEnabled = true;
    }

    public IReadOnlyList<string> Themes { get; } = ["System", "Light", "Dark"];
    public IReadOnlyList<string> BackupFrequencies { get; } = ["Manual", "Daily", "Weekly"];
    public IReadOnlyList<int> LockMinutesOptions { get; } = [1, 2, 5, 10, 15, 30, 60];

    [ObservableProperty] private string _selectedTheme = "System";
    [ObservableProperty] private int _sessionLockMinutes = 5;
    [ObservableProperty] private bool _includeJewelleryInPortfolio = true;
    [ObservableProperty] private string _backupFrequency = "Manual";
    [ObservableProperty] private ObservableCollection<NotificationPref> _notificationPrefs = [];

    public string EncryptionStatus =>
        "On — the database is SQLCipher (AES-256) encrypted and vault passwords use AES-256-GCM. Encryption cannot be disabled.";

    public SettingsViewModel(IAppSettingsService settings, IAssetService assets, InactivityLockService @lock)
    {
        _settings = settings;
        _assets = assets;
        _lock = @lock;
        Load();
    }

    public void Load()
    {
        AppSettingsData data = _settings.Get();
        SelectedTheme = data.Theme;
        SessionLockMinutes = data.SessionLockMinutes;
        IncludeJewelleryInPortfolio = data.IncludeJewelleryInPortfolio;
        BackupFrequency = data.BackupFrequency;
        NotificationPrefs = new ObservableCollection<NotificationPref>(
            Enum.GetValues<NotificationType>().Select(t => new NotificationPref
            {
                Type = t,
                Name = Friendly(t),
                IsEnabled = !data.DisabledNotificationTypes.Contains(t)
            }));
    }

    public void Save()
    {
        var disabled = NotificationPrefs.Where(p => !p.IsEnabled).Select(p => p.Type).ToList();
        _settings.Save(new AppSettingsData(
            SelectedTheme, SessionLockMinutes, IncludeJewelleryInPortfolio, BackupFrequency, disabled,
            _settings.Get().LastBackupUtc));

        ApplyTheme(SelectedTheme);
        _lock.Timeout = TimeSpan.FromMinutes(SessionLockMinutes);
        _assets.SetAllJewelleryExclusion(!IncludeJewelleryInPortfolio);
    }

    /// <summary>Detects the system theme preference from Windows registry.</summary>
    public static string GetSystemTheme()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key?.GetValue("AppsUseLightTheme") is int value && value == 0)
                    return "Dark";
            }
        }
        catch
        {
            // If registry access fails, default to Light
        }
        return "Light";
    }

    public static void ApplyTheme(string theme)
    {
        // If "System" is selected, detect actual system preference
        string actualTheme = theme == "System" ? GetSystemTheme() : theme;

        switch (actualTheme)
        {
            case "Dark": ApplicationThemeManager.Apply(ApplicationTheme.Dark); break;
            case "Light": ApplicationThemeManager.Apply(ApplicationTheme.Light); break;
            default: ApplicationThemeManager.ApplySystemTheme(); break;
        }

        // ApplicationThemeManager.Apply resets the accent to the system accent, so re-brand afterwards.
        ApplyBrandAccent();
        // Update custom theme resources (backgrounds, sidebar colors, etc.)
        Richie.UI.App.UpdateThemeResources();

        // Apply theme-specific overrides for charts, text and status colours
        ApplyThemeOverrides(actualTheme);
    }

    public static void ApplyThemeOverrides(string theme)
    {
        if (System.Windows.Application.Current is null)
            return;

        bool isDark = theme == "Dark";
        var resources = System.Windows.Application.Current.Resources;

        if (isDark)
        {
            // Softer text colors
            resources["TextFillColorPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            resources["TextFillColorSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            resources["TextFillColorTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

            // Status colours using your warm palette
            resources["StatusTealBrush"] = new SolidColorBrush(Color.FromRgb(0x57, 0xB8, 0x94));
            resources["StatusOrangeBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x56));
            resources["StatusAmberBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x56));
            resources["StatusBlueBrush"] = new SolidColorBrush(Color.FromRgb(0x5B, 0x8D, 0xEF));

            resources["BadgeTextBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));
        }
        else
        {
            // Restore light theme defaults
            resources.Remove("TextFillColorPrimaryBrush");
            resources.Remove("TextFillColorSecondaryBrush");
            resources.Remove("TextFillColorTertiaryBrush");

            resources["StatusTealBrush"] = new SolidColorBrush(Color.FromRgb(0x57, 0xB8, 0x94));
            resources["StatusOrangeBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x56));
            resources["StatusAmberBrush"] = new SolidColorBrush(Color.FromRgb(0xE6, 0xA7, 0x56));
            resources["StatusBlueBrush"] = new SolidColorBrush(Color.FromRgb(0x5B, 0x8D, 0xEF));

            resources["BadgeTextBrush"] = Brushes.White;
        }
    }

    /// <summary>Applies a professional brand accent across the app (buttons, nav highlight, focus rings).</summary>
    public static void ApplyBrandAccent()
    {
        ApplicationTheme theme = ApplicationThemeManager.GetAppTheme();
        if (theme is ApplicationTheme.Unknown)
            theme = ApplicationTheme.Light;

        // Apply Soft Golden Orange as the primary accent color
        var accent = (Color)ColorConverter.ConvertFromString("#E6A756")!; // Soft Golden Orange
        ApplicationAccentColorManager.Apply(accent, theme);
    }

    /// <summary>Applies a custom accent color across the app (e.g. Blue for asset dialogs).</summary>
    public static void ApplyAccent(string hexColor)
    {
        var accent = (Color)ColorConverter.ConvertFromString(hexColor)!;
        ApplicationTheme theme = ApplicationThemeManager.GetAppTheme();
        if (theme is ApplicationTheme.Unknown)
            theme = ApplicationTheme.Light;
        ApplicationAccentColorManager.Apply(accent, theme);
    }

    private static string Friendly(NotificationType type) => type switch
    {
        NotificationType.SipReminder => "SIP reminders",
        NotificationType.SipPosted => "SIP posted",
        NotificationType.RecurringExpense => "Recurring expenses",
        NotificationType.InsuranceRenewal => "Insurance renewals",
        NotificationType.PortfolioHealthAlert => "Portfolio health alerts",
        NotificationType.ExpenseAlert => "Expense / budget alerts",
        NotificationType.UploadStatus => "Bulk-upload status",
        NotificationType.GipMaturity => "Guaranteed-plan maturity",
        _ => type.ToString()
    };
}

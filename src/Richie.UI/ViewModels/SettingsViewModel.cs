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
        ApplyThemeOverrides(actualTheme);
    }

    public static void ApplyThemeOverrides(string theme)
    {
        if (System.Windows.Application.Current is null) return;
        
        bool isDark = theme == "Dark";
        var resources = System.Windows.Application.Current.Resources;

        if (isDark)
        {
            // Softer primary, secondary and tertiary text colors for Dark Mode to avoid high contrast look
            resources["TextFillColorPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            resources["TextFillColorSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            resources["TextFillColorTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

            // Status foreground colors
            resources["StatusTealBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0xD4, 0xBF));
            resources["StatusOrangeBrush"] = new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C));
            resources["StatusAmberBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
            resources["StatusBlueBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));

            // Badge text color
            resources["BadgeTextBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));

            // Profit & Loss colors
            resources["ProfitLossPositiveBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0xD4, 0xBF));
            resources["ProfitLossNegativeBrush"] = new SolidColorBrush(Color.FromRgb(0xFB, 0x92, 0x3C));
            resources["ProfitLossPositiveBgBrush"] = new SolidColorBrush(Color.FromArgb(0x1A, 0x2D, 0xD4, 0xBF));
            resources["ProfitLossNegativeBgBrush"] = new SolidColorBrush(Color.FromArgb(0x1A, 0xFB, 0x92, 0x3C));

            // Success cards colors
            resources["SuccessCardBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0x1A, 0x22, 0xC5, 0x5E));
            resources["SuccessCardBorderBrush"] = new SolidColorBrush(Color.FromArgb(0x33, 0x22, 0xC5, 0x5E));
            resources["SuccessCardTextBrush"] = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
            resources["SuccessCardSubTextBrush"] = new SolidColorBrush(Color.FromRgb(0xA7, 0xF3, 0xD0));

            // Lighter Blue brush
            resources["LighterBlueBrush"] = new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA));

            // Chart legend text color paint
            resources["ChartLegendTextPaint"] = new SolidColorPaint(SKColor.Parse("#E2E8F0")) { SKTypeface = SKTypeface.FromFamilyName("Arial") };
        }
        else
        {
            // Remove overrides in Light Mode to fallback to default Light Mode resource dictionary values
            resources.Remove("TextFillColorPrimaryBrush");
            resources.Remove("TextFillColorSecondaryBrush");
            resources.Remove("TextFillColorTertiaryBrush");

            // Status colors
            resources["StatusTealBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E));
            resources["StatusOrangeBrush"] = new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C));
            resources["StatusAmberBrush"] = new SolidColorBrush(Color.FromRgb(0x9D, 0x5D, 0x00));
            resources["StatusBlueBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));

            // Badge text color
            resources["BadgeTextBrush"] = Brushes.White;

            // Profit & Loss colors
            resources["ProfitLossPositiveBrush"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E));
            resources["ProfitLossNegativeBrush"] = new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C));
            resources["ProfitLossPositiveBgBrush"] = new SolidColorBrush(Color.FromRgb(0xCC, 0xFB, 0xF1));
            resources["ProfitLossNegativeBgBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xED, 0xD5));

            // Success cards colors
            resources["SuccessCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
            resources["SuccessCardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xDC, 0xFC, 0xE7));
            resources["SuccessCardTextBrush"] = new SolidColorBrush(Color.FromRgb(0x14, 0x53, 0x2D));
            resources["SuccessCardSubTextBrush"] = new SolidColorBrush(Color.FromRgb(0x16, 0x65, 0x34));

            // Lighter Blue brush
            resources["LighterBlueBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));

            // Chart legend text color paint
            resources["ChartLegendTextPaint"] = new SolidColorPaint(SKColor.Parse("#475569")) { SKTypeface = SKTypeface.FromFamilyName("Arial") };
        }
    }

    /// <summary>Applies a professional brand accent across the app (buttons, nav highlight, focus rings).</summary>
    public static void ApplyBrandAccent()
    {
        ApplicationTheme theme = ApplicationThemeManager.GetAppTheme();
        if (theme is ApplicationTheme.Unknown)
            theme = ApplicationTheme.Light;

        // Use a professional blue/teal accent instead of red for modern UI
        var accent = (Color)ColorConverter.ConvertFromString("#3B82F6")!; // Professional blue
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

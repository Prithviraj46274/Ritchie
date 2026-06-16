using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Vault;
using Richie.UI.Services;

namespace Richie.UI.ViewModels;

/// <summary>
/// Drives the vault page through its locked → setup/unlock → unlocked states. The page re-locks
/// on every navigation (PRD §8.1), so this VM always starts locked and rebuilds its state from
/// the gate on load.
/// </summary>
public partial class PasswordVaultViewModel : ObservableObject
{
    private readonly IVaultGate _gate;
    private readonly IVaultService _vault;
    private readonly IVaultRevealStateService _revealState;
    private readonly DispatcherTimer _lockTimer;
    private DateTime _lockDeadlineUtc;
    private bool _suppressReload;

    private static readonly TimeSpan AutoLockTimeout = TimeSpan.FromMinutes(5);

    [ObservableProperty] private bool _isUnlocked;
    [ObservableProperty] private bool _isLocked = true;
    [ObservableProperty] private bool _isSetupMode;
    [ObservableProperty] private string _masterPassword = string.Empty;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _recoveryAvailable;
    [ObservableProperty] private bool _allRevealed;
    [ObservableProperty] private ObservableCollection<VaultEntryRowViewModel> _items = [];
    [ObservableProperty] private string _unlockTimerText = string.Empty;

    private const string AllCategories = "All categories";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _categories = [AllCategories];
    [ObservableProperty] private string _selectedCategory = AllCategories;

    public string GateHeading => IsSetupMode ? "Create your vault master password" : "Unlock your vault";

    public string GateDescription => IsSetupMode
        ? "Set a master password to encrypt your credentials. You'll enter it each time you open the vault."
        : "Enter your vault master password. The vault re-locks every time you leave this page.";

    public string SubmitText => IsSetupMode ? "Create vault" : "Unlock";

    public PasswordVaultViewModel(IVaultGate gate, IVaultService vault, IVaultRevealStateService revealState)
    {
        _gate = gate;
        _vault = vault;
        _revealState = revealState;

        _lockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _lockTimer.Tick += (_, _) => UpdateLockCountdown();
    }

    /// <summary>Re-lock and reset to the gate screen — called whenever the page is shown.</summary>
    public void ResetToLocked()
    {
        _gate.Lock();
        _suppressReload = true;
        MasterPassword = string.Empty;
        Error = null;
        IsSetupMode = !_gate.IsConfigured();
        RecoveryAvailable = !IsSetupMode && _gate.IsRecoveryEnabled();
        SetUnlocked(false);
        Items = [];
        SearchText = string.Empty;
        Categories = [AllCategories];
        SelectedCategory = AllCategories;
        UnlockTimerText = string.Empty;
        _revealState.Clear();
        _suppressReload = false;
    }

    public void Submit()
    {
        Error = null;
        VaultUnlockResult result = IsSetupMode
            ? _gate.SetupMasterPassword(MasterPassword)
            : _gate.Unlock(MasterPassword);

        if (!result.IsSuccess)
        {
            Error = result.Message ?? "Could not unlock the vault.";
            return;
        }

        MasterPassword = string.Empty;
        SetUnlocked(true);
        Reload();
    }

    public void Reload()
    {
        RefreshCategories();
        string? category = SelectedCategory == AllCategories ? null : SelectedCategory;
        string? search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText;
        var revealedIds = _revealState.GetRevealedEntryIds().ToHashSet();
        var entries = _vault.GetEntries(search, category)
            .Select(s => new VaultEntryRowViewModel(
                s,
                _revealState,
                revealedIds.Contains(s.Id) ? _vault.RevealPassword(s.Id) : null));
        Items = new ObservableCollection<VaultEntryRowViewModel>(entries);
        AllRevealed = Items.Count > 0 && Items.All(i => i.IsRevealed);
    }

    /// <summary>Decrypt a credential for inline reveal/copy — caller must have re-authenticated.</summary>
    public string? RevealPassword(Guid id) => _vault.RevealPassword(id);

    public void ToggleRevealAll()
    {
        if (AllRevealed)
        {
            HideAll();
            return;
        }

        foreach (VaultEntryRowViewModel row in Items)
        {
            if (!row.IsRevealed && RevealPassword(row.Id) is { Length: > 0 } plaintext)
                row.Reveal(plaintext);
        }

        AllRevealed = true;
    }

    public void HideAll()
    {
        foreach (VaultEntryRowViewModel row in Items)
            row.Hide();

        AllRevealed = false;
        _revealState.Clear();
    }

    /// <summary>Transition to the unlocked state after a recovery unlock (gate is already unlocked).</summary>
    public void MarkUnlocked()
    {
        MasterPassword = string.Empty;
        Error = null;
        SetUnlocked(true);
        Reload();
    }

    public void Delete(Guid id)
    {
        _vault.Delete(id);
        Reload();
    }

    private void RefreshCategories()
    {
        string current = SelectedCategory;
        var list = new ObservableCollection<string> { AllCategories };
        foreach (string c in _vault.GetCategories())
            list.Add(c);

        _suppressReload = true;
        Categories = list;
        SelectedCategory = list.Contains(current) ? current : AllCategories;
        _suppressReload = false;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (!_suppressReload) Reload();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        if (!_suppressReload) Reload();
    }

    private void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
        IsLocked = !unlocked;
        if (unlocked)
            StartLockTimer();
        else
        {
            AllRevealed = false;
            StopLockTimer();
        }
    }

    private void StartLockTimer()
    {
        _lockDeadlineUtc = DateTime.UtcNow.Add(AutoLockTimeout);
        UpdateLockCountdown();
        _lockTimer.Start();
    }

    private void StopLockTimer()
    {
        _lockTimer.Stop();
        UnlockTimerText = string.Empty;
    }

    private void UpdateLockCountdown()
    {
        TimeSpan remaining = _lockDeadlineUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            ResetToLocked();
            return;
        }

        UnlockTimerText = $"Auto-lock in {remaining:mm\\:ss}";
    }

    public string RevealAllText => AllRevealed ? "Hide all" : "Reveal all";

    partial void OnIsSetupModeChanged(bool value)
    {
        OnPropertyChanged(nameof(GateHeading));
        OnPropertyChanged(nameof(GateDescription));
        OnPropertyChanged(nameof(SubmitText));
    }

    partial void OnAllRevealedChanged(bool value)
    {
        OnPropertyChanged(nameof(RevealAllText));
    }
}

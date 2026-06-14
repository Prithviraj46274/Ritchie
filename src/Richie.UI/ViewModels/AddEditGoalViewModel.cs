using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Assets;
using Richie.Domain.Assets;

namespace Richie.UI.ViewModels;

public partial class AddEditGoalViewModel : ObservableObject
{
    private readonly IGoalService _goals;
    private readonly IAssetService _assets;
    private Guid? _editId;

    public sealed class AssetPick
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsLinked { get; set; }
    }

    public sealed record PriorityOption(GoalPriority Value, string Text);

    public IReadOnlyList<PriorityOption> Priorities { get; } =
    [
        new(GoalPriority.High, "High"),
        new(GoalPriority.Medium, "Medium"),
        new(GoalPriority.Low, "Low"),
    ];

    [ObservableProperty] private string _title = "Add goal";
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _targetAmountText = string.Empty;
    [ObservableProperty] private DateTime? _targetDate = DateTime.Today.AddYears(1);
    [ObservableProperty] private GoalPriority _priority = GoalPriority.Medium;
    [ObservableProperty] private ObservableCollection<AssetPick> _assetPicks = [];
    [ObservableProperty] private string? _error;

    public event Action<bool>? CloseRequested;

    public AddEditGoalViewModel(IGoalService goals, IAssetService assets)
    {
        _goals = goals;
        _assets = assets;
    }

    public void Initialize(Guid? id)
    {
        _editId = id;
        GoalEditData? existing = id is null ? null : _goals.GetGoal(id.Value);
        IReadOnlyList<Guid> linked = existing?.LinkedAssetIds ?? [];

        if (existing is not null)
        {
            Title = "Edit goal";
            Name = existing.Name;
            TargetAmountText = existing.TargetAmount.ToString(CultureInfo.CurrentCulture);
            TargetDate = existing.TargetDate;
            Priority = existing.Priority;
        }

        AssetPicks = new ObservableCollection<AssetPick>(_assets.GetAssets().Select(a => new AssetPick
        {
            Id = a.Id,
            Name = $"{a.Name} ({a.TypeName})",
            IsLinked = linked.Contains(a.Id)
        }));
    }

    [RelayCommand]
    private void Save()
    {
        Error = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            Error = "Goal name is required.";
            return;
        }
        if (!decimal.TryParse(TargetAmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal target) || target <= 0)
        {
            Error = "Enter a valid target amount.";
            return;
        }
        if (TargetDate is null)
        {
            Error = "Target date is required.";
            return;
        }

        var input = new GoalInput(Name, target, TargetDate.Value, Priority);
        List<Guid> linkedIds = AssetPicks.Where(p => p.IsLinked).Select(p => p.Id).ToList();

        if (_editId is null)
            _goals.CreateGoal(input, linkedIds);
        else
            _goals.UpdateGoal(_editId.Value, input, linkedIds);

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}

using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Assets;
using Richie.Application.Common;
using Richie.Domain.Assets;

namespace Richie.UI.ViewModels;

public partial class AddEditAssetViewModel : ObservableObject
{
    private readonly IAssetService _assets;
    private readonly IAssetDocumentService _docs;
    private Guid? _editId;

    public sealed record TypeOption(AssetType Value, string Text);

    public IReadOnlyList<TypeOption> Types { get; } =
        Enum.GetValues<AssetType>().Select(t => new TypeOption(t, AssetTypeNames.Display(t))).ToList();

    [ObservableProperty] private string _title = "Add asset";
    [ObservableProperty] private AssetType _selectedType = AssetType.MutualFund;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _identifier;
    [ObservableProperty] private DateTime? _investmentStartDate = DateTime.Today;
    [ObservableProperty] private string _investedAmountText = string.Empty;
    [ObservableProperty] private string _quantityText = string.Empty;
    [ObservableProperty] private string _purchasePriceText = string.Empty;
    [ObservableProperty] private string _currentValueText = string.Empty;
    [ObservableProperty] private DateTime? _valuationDate = DateTime.Today;
    [ObservableProperty] private bool _isSip;
    [ObservableProperty] private string? _notes;

    // Type-specific
    [ObservableProperty] private string? _exchange;
    [ObservableProperty] private string _issuePriceText = string.Empty;
    [ObservableProperty] private DateTime? _maturityDate;
    [ObservableProperty] private string? _platformName;
    [ObservableProperty] private string? _propertyAddress;
    [ObservableProperty] private string _areaText = string.Empty;
    [ObservableProperty] private string _weightText = string.Empty;
    [ObservableProperty] private string? _purity;
    [ObservableProperty] private string? _appraiserName;
    [ObservableProperty] private string? _policyNumber;
    [ObservableProperty] private string _guaranteedReturnText = string.Empty;
    [ObservableProperty] private bool _isExcludedFromPortfolio;

    [ObservableProperty] private string? _error;
    [ObservableProperty] private string _imageUploadStatus = string.Empty;

    public AddEditAssetViewModel(IAssetService assets, IAssetDocumentService docs)
    {
        _assets = assets;
        _docs = docs;
    }

    public event Action<bool>? CloseRequested;

    // Field visibility driven by the selected type (PRD §6.1 dynamic form).
    public bool ShowIdentifier => SelectedType is AssetType.MutualFund or AssetType.Equity or AssetType.SovereignGoldBond;
    public bool ShowQuantityPrice => SelectedType is AssetType.MutualFund or AssetType.Equity or AssetType.SovereignGoldBond or AssetType.DigitalGold;
    public bool ShowExchange => SelectedType is AssetType.Equity;
    public bool ShowIssuePrice => SelectedType is AssetType.SovereignGoldBond;
    public bool ShowMaturityDate => SelectedType is AssetType.SovereignGoldBond or AssetType.GuaranteedInvestmentPlan;
    public bool ShowPlatform => SelectedType is AssetType.DigitalGold;
    public bool ShowRealEstate => SelectedType is AssetType.RealEstate;
    public bool ShowJewellery => SelectedType is AssetType.GoldJewellery;
    public bool ShowPolicy => SelectedType is AssetType.GuaranteedInvestmentPlan;

    partial void OnSelectedTypeChanged(AssetType value)
    {
        OnPropertyChanged(nameof(ShowIdentifier));
        OnPropertyChanged(nameof(ShowQuantityPrice));
        OnPropertyChanged(nameof(ShowExchange));
        OnPropertyChanged(nameof(ShowIssuePrice));
        OnPropertyChanged(nameof(ShowMaturityDate));
        OnPropertyChanged(nameof(ShowPlatform));
        OnPropertyChanged(nameof(ShowRealEstate));
        OnPropertyChanged(nameof(ShowJewellery));
        OnPropertyChanged(nameof(ShowPolicy));
    }

    public void Initialize(Guid? id)
    {
        _editId = id;
        if (id is null)
        {
            Title = "Add asset";
            return;
        }

        Asset? a = _assets.GetById(id.Value);
        if (a is null)
            return;

        Title = "Edit asset";
        SelectedType = a.Type;
        Name = a.Name;
        Identifier = a.Identifier;
        InvestmentStartDate = a.InvestmentStartDate;
        InvestedAmountText = NumberFormatter.FormatIndianWhole(a.InvestedAmount);
        QuantityText = a.Quantity?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        PurchasePriceText = a.PurchasePricePerUnit?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        CurrentValueText = NumberFormatter.FormatIndianWhole(a.CurrentValue);
        ValuationDate = a.ValuationDate;
        IsSip = a.InvestmentMode == InvestmentMode.Sip;
        Notes = a.Notes;
        Exchange = a.Exchange;
        IssuePriceText = a.IssuePrice?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        MaturityDate = a.MaturityDate;
        PlatformName = a.PlatformName;
        PropertyAddress = a.PropertyAddress;
        AreaText = a.AreaSquareFeet?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        WeightText = a.Weight?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        Purity = a.Purity;
        AppraiserName = a.AppraiserName;
        PolicyNumber = a.PolicyNumber;
        GuaranteedReturnText = a.GuaranteedReturnPercent?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        IsExcludedFromPortfolio = a.IsExcludedFromPortfolio;
    }

    [RelayCommand]
    private void Save()
    {
        Error = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            Error = "Asset name is required.";
            return;
        }
        if (!TryParse(InvestedAmountText, out decimal invested) || invested < 0)
        {
            Error = "Enter a valid invested amount.";
            return;
        }
        if (!TryParse(CurrentValueText, out decimal currentValue) || currentValue < 0)
        {
            Error = "Enter a valid current value.";
            return;
        }
        if (InvestmentStartDate is null)
        {
            Error = "Investment start date is required.";
            return;
        }

        var input = new AssetInput
        {
            Type = SelectedType,
            Name = Name,
            Identifier = Identifier,
            InvestmentStartDate = InvestmentStartDate.Value,
            InvestedAmount = invested,
            Quantity = ParseNullable(QuantityText),
            PurchasePricePerUnit = ParseNullable(PurchasePriceText),
            CurrentValue = currentValue,
            ValuationDate = ValuationDate,
            InvestmentMode = IsSip ? InvestmentMode.Sip : InvestmentMode.LumpSum,
            Notes = Notes,
            IsExcludedFromPortfolio = IsExcludedFromPortfolio,
            Exchange = Exchange,
            IssuePrice = ParseNullable(IssuePriceText),
            MaturityDate = MaturityDate,
            PlatformName = PlatformName,
            PropertyAddress = PropertyAddress,
            AreaSquareFeet = ParseNullable(AreaText),
            Weight = ParseNullable(WeightText),
            Purity = Purity,
            AppraiserName = AppraiserName,
            PolicyNumber = PolicyNumber,
            GuaranteedReturnPercent = ParseNullable(GuaranteedReturnText),
        };

        if (_editId is null)
            _assets.Create(input);
        else
            _assets.Update(_editId.Value, input);

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    [RelayCommand]
    private async System.Threading.Tasks.Task UploadImageAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp|All files|*.*",
                Title = "Select an image to upload"
            };

            if (dialog.ShowDialog() == true)
            {
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(dialog.FileName);
                
                // If editing, upload to existing asset. If adding, we'll need to save first.
                if (_editId is not null)
                {
                    _docs.Attach(_editId.Value, dialog.FileName, fileBytes);
                    ImageUploadStatus = $"Image '{System.IO.Path.GetFileName(dialog.FileName)}' uploaded successfully.";
                }
                else
                {
                    ImageUploadStatus = "Please save the asset first before uploading images.";
                }
            }
        }
        catch (Exception ex)
        {
            ImageUploadStatus = $"Error uploading image: {ex.Message}";
        }
    }

    private static bool TryParse(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);

    private static decimal? ParseNullable(string text) =>
        TryParse(text, out decimal value) ? value : null;
}

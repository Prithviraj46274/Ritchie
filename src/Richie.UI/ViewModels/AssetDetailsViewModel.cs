using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Richie.Application.Assets;
using Richie.Application.Common;
using Richie.Domain.Assets;

namespace Richie.UI.ViewModels;

public partial class AssetDetailsViewModel : ObservableObject
{
    private readonly IAssetService _assets;
    private readonly IValuationService _valuation;
    private readonly IGoalService _goals;
    private readonly IAssetDocumentService _docs;
    private Guid _assetId;
    private bool _loading;

    public sealed record DetailRow(string Label, string Value);
    public sealed record AssetImageItem(Guid Id, string FileName, BitmapImage Thumbnail);

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _typeName = string.Empty;
    [ObservableProperty] private string _categoryName = string.Empty;
    [ObservableProperty] private ObservableCollection<DetailRow> _rows = [];
    [ObservableProperty] private ObservableCollection<AssetImageItem> _images = [];
    [ObservableProperty] private bool _isJewellery;
    [ObservableProperty] private bool _isExcluded;
    [ObservableProperty] private string _profitLossText = string.Empty;
    [ObservableProperty] private Brush _profitLossBrush = Brushes.Gray;
    [ObservableProperty] private bool _hasImages;

    private static readonly Brush Green = Frozen(Color.FromRgb(0x0F, 0x7B, 0x0F));
    private static readonly Brush Red = Frozen(Color.FromRgb(0xC4, 0x2B, 0x1C));

    public bool EditRequested { get; private set; }

    public event Action? CloseRequested;

    public AssetDetailsViewModel(IAssetService assets, IValuationService valuation, IGoalService goals, IAssetDocumentService docs)
    {
        _assets = assets;
        _valuation = valuation;
        _goals = goals;
        _docs = docs;
    }

    public async void Initialize(Guid id)
    {
        Asset? a = _assets.GetById(id);
        if (a is null)
            return;

        _loading = true;
        _assetId = a.Id;
        Name = a.Name;
        TypeName = AssetTypeNames.Display(a.Type);
        CategoryName = a.Type.GetCategory().ToString();
        IsJewellery = a.Type == AssetType.GoldJewellery;
        IsExcluded = a.IsExcludedFromPortfolio;

        decimal pl = _valuation.ProfitLoss(a.InvestedAmount, a.CurrentValue);
        decimal plPercent = _valuation.ProfitLossPercent(a.InvestedAmount, a.CurrentValue);
        ProfitLossText = $"{Money(pl)} ({plPercent:+0.0;-0.0;0.0}%)";
        ProfitLossBrush = pl < 0 ? Red : Green;

        var rows = new List<DetailRow>
        {
            new("Type", TypeName),
            new("Category", CategoryName),
            new("Invested", Money(a.InvestedAmount)),
            new("Current value", Money(a.CurrentValue)),
            new("Investment mode", a.InvestmentMode == InvestmentMode.Sip ? "SIP (recurring)" : "Lump sum"),
            new("Start date", DateFormatter.Format(a.InvestmentStartDate)),
        };

        AddIf(rows, "ISIN / Ticker", a.Identifier);
        AddIf(rows, "Exchange", a.Exchange);
        AddIf(rows, "Quantity / Units", a.Quantity);
        AddIf(rows, "Purchase price / unit", a.PurchasePricePerUnit);
        AddIf(rows, "Issue price", a.IssuePrice);
        AddIf(rows, "Maturity date", DateFormatter.Format(a.MaturityDate));
        AddIf(rows, "Platform", a.PlatformName);
        AddIf(rows, "Property address", a.PropertyAddress);
        AddIf(rows, "Area (sq ft)", a.AreaSquareFeet);
        AddIf(rows, "Weight (g)", a.Weight);
        AddIf(rows, "Purity", a.Purity);
        AddIf(rows, "Appraiser", a.AppraiserName);
        AddIf(rows, "Policy number", a.PolicyNumber);
        AddIf(rows, "Guaranteed return %", a.GuaranteedReturnPercent);
        AddIf(rows, "Valuation date", DateFormatter.Format(a.ValuationDate));
        AddIf(rows, "Notes", a.Notes);

        IReadOnlyList<string> goalNames = _goals.GetGoalNamesForAsset(a.Id);
        if (goalNames.Count > 0)
            rows.Add(new DetailRow("Goals", string.Join(", ", goalNames)));

        Rows = new ObservableCollection<DetailRow>(rows);
        
        // Load images asynchronously
        await LoadImagesAsync(id);
        
        _loading = false;
    }

    private async System.Threading.Tasks.Task LoadImagesAsync(Guid assetId)
    {
        try
        {
            var documents = _docs.GetForAsset(assetId);
            var imageItems = new List<AssetImageItem>();

            foreach (var doc in documents.Where(d => d.Kind == Domain.Assets.DocumentKind.Image))
            {
                try
                {
                    byte[] content = _docs.OpenContent(doc.Id);
                    var bitmap = await LoadBitmapAsync(content);
                    if (bitmap != null)
                    {
                        imageItems.Add(new AssetImageItem(doc.Id, doc.FileName, bitmap));
                    }
                }
                catch
                {
                    // Skip images that fail to load
                    continue;
                }
            }

            Images = new ObservableCollection<AssetImageItem>(imageItems);
            HasImages = Images.Count > 0;
        }
        catch
        {
            HasImages = false;
        }
    }

    private async System.Threading.Tasks.Task<BitmapImage?> LoadBitmapAsync(byte[] content)
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(content))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.DecodePixelWidth = 300; // Thumbnail size
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        });
    }

    partial void OnIsExcludedChanged(bool value)
    {
        if (_loading)
            return;
        _assets.SetPortfolioExclusion(_assetId, value);
    }

    [RelayCommand]
    private void Edit()
    {
        EditRequested = true;
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();

    private static void AddIf(List<DetailRow> rows, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            rows.Add(new DetailRow(label, value));
    }

    private static void AddIf(List<DetailRow> rows, string label, decimal? value)
    {
        if (value is not null)
            rows.Add(new DetailRow(label, Money(value.Value)));
    }

    private static string Money(decimal value) => CurrencyFormatter.Format(value);

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

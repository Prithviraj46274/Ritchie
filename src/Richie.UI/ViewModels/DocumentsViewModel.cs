using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Assets;
using Richie.Domain.Assets;

namespace Richie.UI.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    private readonly IAssetDocumentService _docs;
    private Guid _assetId;

    [ObservableProperty] private string _assetName = string.Empty;
    [ObservableProperty] private ObservableCollection<AssetDocumentDto> _items = [];
    [ObservableProperty] private AssetDocumentDto? _selectedDocument;
    [ObservableProperty] private ImageSource? _previewImage;
    [ObservableProperty] private string _previewMessage = "Select a document to preview.";
    [ObservableProperty] private bool _isEmpty;

    public DocumentsViewModel(IAssetDocumentService docs) => _docs = docs;

    public void Initialize(Guid assetId, string assetName)
    {
        _assetId = assetId;
        AssetName = assetName;
        Refresh();
    }

    public void Refresh()
    {
        Items = new ObservableCollection<AssetDocumentDto>(_docs.GetForAsset(_assetId));
        IsEmpty = Items.Count == 0;
        SelectedDocument = Items.FirstOrDefault();
    }

    public void Attach(string filePath)
    {
        _docs.Attach(_assetId, Path.GetFileName(filePath), File.ReadAllBytes(filePath));
        Refresh();
    }

    public void Download(string destinationPath)
    {
        if (SelectedDocument is { } doc)
            File.WriteAllBytes(destinationPath, _docs.OpenContent(doc.Id));
    }

    public void DeleteSelected()
    {
        if (SelectedDocument is { } doc)
        {
            _docs.Delete(doc.Id);
            Refresh();
        }
    }

    public void SelectPrevious() => MoveSelection(-1);
    public void SelectNext() => MoveSelection(1);

    private void MoveSelection(int delta)
    {
        if (SelectedDocument is null)
            return;
        int index = Items.IndexOf(SelectedDocument) + delta;
        if (index >= 0 && index < Items.Count)
            SelectedDocument = Items[index];
    }

    partial void OnSelectedDocumentChanged(AssetDocumentDto? value)
    {
        PreviewImage = null;
        if (value is null)
        {
            PreviewMessage = "Select a document to preview.";
            return;
        }

        if (value.Kind == DocumentKind.Image)
        {
            try
            {
                PreviewImage = LoadImage(_docs.OpenContent(value.Id));
                PreviewMessage = string.Empty;
            }
            catch
            {
                PreviewMessage = "Could not open this image.";
            }
        }
        else
        {
            PreviewMessage = "No inline preview for this file type. Use Download to save a copy.";
        }
    }

    private static ImageSource LoadImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}

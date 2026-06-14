using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Expenses;
using Richie.Domain.Assets;

namespace Richie.UI.ViewModels;

/// <summary>Bills/receipts attached to an expense (mirrors DocumentsViewModel; encrypted at rest).</summary>
public partial class BillsViewModel : ObservableObject
{
    private readonly IExpenseDocumentService _docs;
    private Guid _expenseId;

    public const string PreviewableTypes = "Previewable: PNG, JPG, JPEG, BMP, GIF, WEBP. Other files (e.g. PDF): use Download.";

    [ObservableProperty] private string _heading = "Bills & receipts";
    [ObservableProperty] private ObservableCollection<ExpenseDocumentDto> _items = [];
    [ObservableProperty] private ExpenseDocumentDto? _selectedDocument;
    [ObservableProperty] private ImageSource? _previewImage;
    [ObservableProperty] private string _previewMessage = "Select a bill to preview.";
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _hasMultiple;

    public BillsViewModel(IExpenseDocumentService docs) => _docs = docs;

    public void Initialize(Guid expenseId, string heading)
    {
        _expenseId = expenseId;
        Heading = heading;
        Refresh();
    }

    public void Refresh()
    {
        Items = new ObservableCollection<ExpenseDocumentDto>(_docs.GetForExpense(_expenseId));
        IsEmpty = Items.Count == 0;
        HasMultiple = Items.Count > 1;
        SelectedDocument = Items.FirstOrDefault();
    }

    public void Attach(string filePath) =>
        _docs.Attach(_expenseId, Path.GetFileName(filePath), File.ReadAllBytes(filePath));

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

    partial void OnSelectedDocumentChanged(ExpenseDocumentDto? value)
    {
        PreviewImage = null;
        if (value is null)
        {
            PreviewMessage = "Select a bill to preview.";
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
            PreviewMessage = $"No inline preview for a {value.Kind} file. Use Download to save a copy.\n\n{PreviewableTypes}";
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

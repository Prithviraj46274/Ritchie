using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Richie.Application.Assets;

namespace Richie.UI.ViewModels;

public partial class BulkUploadViewModel : ObservableObject
{
    private readonly IAssetImportService _import;

    [ObservableProperty] private string _summary = "Download a template, fill it in, then drop the file here or browse.";
    [ObservableProperty] private ObservableCollection<ImportRowError> _errors = [];
    [ObservableProperty] private bool _hasErrors;

    public bool ImportedAny { get; private set; }

    public BulkUploadViewModel(IAssetImportService import) => _import = import;

    public void ImportFile(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        using FileStream stream = File.OpenRead(filePath);

        ImportResult result;
        switch (ext)
        {
            case ".csv":
                result = _import.ImportCsv(stream);
                break;
            case ".xlsx":
                result = _import.ImportExcel(stream);
                break;
            default:
                Summary = "Unsupported file type. Use a .csv or .xlsx file.";
                Errors = [];
                HasErrors = false;
                return;
        }

        Summary = $"Imported {result.ImportedCount} of {result.TotalRows} row(s)" +
                  (result.HasErrors ? $", {result.Errors.Count} row(s) had errors:" : ".");
        Errors = new ObservableCollection<ImportRowError>(result.Errors);
        HasErrors = result.HasErrors;
        ImportedAny |= result.ImportedCount > 0;
    }

    public void SaveCsvTemplate(string path) => File.WriteAllBytes(path, _import.CreateCsvTemplate());

    public void SaveExcelTemplate(string path) => File.WriteAllBytes(path, _import.CreateExcelTemplate());
}

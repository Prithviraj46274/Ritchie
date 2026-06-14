namespace Richie.Application.Assets;

public sealed record ImportRowError(int RowNumber, string Message);

public sealed record ImportResult(int ImportedCount, int TotalRows, IReadOnlyList<ImportRowError> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Bulk import of assets from CSV/Excel, with row-by-row validation, plus template generation (PRD §6.4).
/// Valid rows are imported even when other rows fail.
/// </summary>
public interface IAssetImportService
{
    ImportResult ImportCsv(Stream csv);
    ImportResult ImportExcel(Stream xlsx);

    byte[] CreateCsvTemplate();
    byte[] CreateExcelTemplate();
}

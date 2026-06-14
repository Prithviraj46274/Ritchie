using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using Richie.Application.Assets;
using Richie.Domain.Assets;

namespace Richie.Infrastructure.Assets;

public sealed class AssetImportService : IAssetImportService
{
    private readonly IAssetService _assets;

    public AssetImportService(IAssetService assets) => _assets = assets;

    public ImportResult ImportCsv(Stream csv) => Import(ReadCsv(csv));

    public ImportResult ImportExcel(Stream xlsx) => Import(ReadExcel(xlsx));

    public byte[] CreateCsvTemplate()
    {
        return new UTF8Encoding(false).GetBytes(string.Join(",", AssetImportColumns.All) + Environment.NewLine);
    }

    public byte[] CreateExcelTemplate()
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.AddWorksheet("Assets");
        for (int i = 0; i < AssetImportColumns.All.Count; i++)
            sheet.Cell(1, i + 1).Value = AssetImportColumns.All[i];
        sheet.Row(1).Style.Font.Bold = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private ImportResult Import(List<(int RowNumber, Dictionary<string, string> Values)> rows)
    {
        var errors = new List<ImportRowError>();
        var inputs = new List<AssetInput>();

        foreach ((int rowNumber, Dictionary<string, string> values) in rows)
        {
            AssetInput? input = TryBuild(values, out string? error);
            if (input is null)
                errors.Add(new ImportRowError(rowNumber, error!));
            else
                inputs.Add(input);
        }

        foreach (AssetInput input in inputs)
            _assets.Create(input);

        return new ImportResult(inputs.Count, rows.Count, errors);
    }

    private static List<(int, Dictionary<string, string>)> ReadCsv(Stream csv)
    {
        var rows = new List<(int, Dictionary<string, string>)>();
        using var reader = new StreamReader(csv);
        using var parser = new CsvReader(reader, CultureInfo.InvariantCulture);

        parser.Read();
        parser.ReadHeader();
        string[] headers = parser.HeaderRecord ?? [];

        int rowNumber = 1; // header is row 1
        while (parser.Read())
        {
            rowNumber++;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string header in headers)
                values[header] = parser.GetField(header) ?? string.Empty;
            rows.Add((rowNumber, values));
        }
        return rows;
    }

    private static List<(int, Dictionary<string, string>)> ReadExcel(Stream xlsx)
    {
        var rows = new List<(int, Dictionary<string, string>)>();
        using var workbook = new XLWorkbook(xlsx);
        IXLWorksheet sheet = workbook.Worksheet(1);
        IXLRangeRows? used = sheet.RangeUsed()?.RowsUsed();
        if (used is null)
            return rows;

        List<IXLRangeRow> rowList = used.ToList();
        if (rowList.Count == 0)
            return rows;

        // Map column number -> header name from the first used row.
        var headerColumns = new List<(int Column, string Name)>();
        foreach (IXLCell cell in rowList[0].CellsUsed())
            headerColumns.Add((cell.Address.ColumnNumber, cell.GetString()));

        for (int i = 1; i < rowList.Count; i++)
        {
            IXLRangeRow row = rowList[i];
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach ((int column, string name) in headerColumns)
                values[name] = row.Worksheet.Cell(row.RowNumber(), column).GetString();
            rows.Add((row.RowNumber(), values));
        }
        return rows;
    }

    private static AssetInput? TryBuild(Dictionary<string, string> v, out string? error)
    {
        error = null;

        string typeRaw = Get(v, AssetImportColumns.Type);
        if (!Enum.TryParse(typeRaw, ignoreCase: true, out AssetType type) || !Enum.IsDefined(type))
        {
            error = $"Unknown asset type '{typeRaw}'.";
            return null;
        }

        string name = Get(v, AssetImportColumns.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Name is required.";
            return null;
        }

        if (!TryDate(Get(v, AssetImportColumns.InvestmentStartDate), out DateTime start))
        {
            error = "Invalid or missing InvestmentStartDate (use yyyy-MM-dd).";
            return null;
        }

        if (!TryDecimal(Get(v, AssetImportColumns.InvestedAmount), out decimal invested))
        {
            error = "Invalid or missing InvestedAmount.";
            return null;
        }

        string currentRaw = Get(v, AssetImportColumns.CurrentValue);
        decimal current;
        if (string.IsNullOrWhiteSpace(currentRaw))
            current = invested;
        else if (!TryDecimal(currentRaw, out current))
        {
            error = "Invalid CurrentValue.";
            return null;
        }

        InvestmentMode mode = Get(v, AssetImportColumns.InvestmentMode).Trim()
            .Equals("SIP", StringComparison.OrdinalIgnoreCase)
            ? InvestmentMode.Sip
            : InvestmentMode.LumpSum;

        return new AssetInput
        {
            Type = type,
            Name = name,
            Identifier = Opt(v, AssetImportColumns.Identifier),
            InvestmentStartDate = start,
            InvestedAmount = invested,
            Quantity = OptDecimal(v, AssetImportColumns.Quantity),
            PurchasePricePerUnit = OptDecimal(v, AssetImportColumns.PurchasePricePerUnit),
            CurrentValue = current,
            ValuationDate = OptDate(v, AssetImportColumns.ValuationDate),
            InvestmentMode = mode,
            Notes = Opt(v, AssetImportColumns.Notes),
            Exchange = Opt(v, AssetImportColumns.Exchange),
            IssuePrice = OptDecimal(v, AssetImportColumns.IssuePrice),
            MaturityDate = OptDate(v, AssetImportColumns.MaturityDate),
            PlatformName = Opt(v, AssetImportColumns.PlatformName),
            PropertyAddress = Opt(v, AssetImportColumns.PropertyAddress),
            AreaSquareFeet = OptDecimal(v, AssetImportColumns.AreaSquareFeet),
            Weight = OptDecimal(v, AssetImportColumns.Weight),
            Purity = Opt(v, AssetImportColumns.Purity),
            AppraiserName = Opt(v, AssetImportColumns.AppraiserName),
            PolicyNumber = Opt(v, AssetImportColumns.PolicyNumber),
            GuaranteedReturnPercent = OptDecimal(v, AssetImportColumns.GuaranteedReturnPercent),
        };
    }

    private static string Get(Dictionary<string, string> v, string key) =>
        v.TryGetValue(key, out string? value) ? value.Trim() : string.Empty;

    private static string? Opt(Dictionary<string, string> v, string key)
    {
        string value = Get(v, key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static decimal? OptDecimal(Dictionary<string, string> v, string key) =>
        TryDecimal(Get(v, key), out decimal d) ? d : null;

    private static DateTime? OptDate(Dictionary<string, string> v, string key) =>
        TryDate(Get(v, key), out DateTime d) ? d : null;

    private static bool TryDecimal(string s, out decimal value) =>
        decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out value);

    private static bool TryDate(string s, out DateTime value) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
        || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
}

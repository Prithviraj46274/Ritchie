using System.IO;
using System.Text;
using Richie.Application.Assets;
using Richie.Infrastructure.Assets;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Assets;

public sealed class AssetImportServiceTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new();
    private readonly UserSession _session = new();
    private readonly AssetService _assets;
    private readonly AssetImportService _sut;

    public AssetImportServiceTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _assets = new AssetService(_db, new ValuationService(), _session, _clock);
        _sut = new AssetImportService(_assets);
    }

    private static Stream Csv(string content) => new MemoryStream(Encoding.UTF8.GetBytes(content));

    [Fact]
    public void ImportCsv_ImportsValidRows_AndReportsInvalidOnes()
    {
        // Header + 3 rows: valid, bad type, missing name.
        string csv = string.Join("\n",
            "Type,Name,InvestmentStartDate,InvestedAmount,CurrentValue,InvestmentMode",
            "MutualFund,HDFC Flexicap,2026-01-01,1000,1200,SIP",
            "Crypto,Some Coin,2026-01-01,500,500,LumpSum",
            "Equity,,2026-01-01,500,500,LumpSum");

        ImportResult result = _sut.ImportCsv(Csv(csv));

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.RowNumber == 3 && e.Message.Contains("type"));
        Assert.Contains(result.Errors, e => e.RowNumber == 4 && e.Message.Contains("Name"));

        AssetSummary imported = Assert.Single(_assets.GetAssets());
        Assert.Equal("HDFC Flexicap", imported.Name);
        Assert.Equal(200m, imported.ProfitLoss);
    }

    [Fact]
    public void ImportCsv_DefaultsCurrentValueToInvested_WhenBlank()
    {
        string csv = string.Join("\n",
            "Type,Name,InvestmentStartDate,InvestedAmount",
            "Equity,Acme,2026-01-01,750");

        ImportResult result = _sut.ImportCsv(Csv(csv));

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(750m, _assets.GetAssets().Single().CurrentValue);
    }

    [Fact]
    public void ImportExcel_RoundTripsThroughGeneratedTemplate()
    {
        // Build a workbook from the real template, fill one data row, import it.
        byte[] template = _sut.CreateExcelTemplate();
        using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(template));
        var sheet = wb.Worksheet(1);

        // Header row 1 already has column names; columns are in AssetImportColumns order.
        sheet.Cell(2, 1).Value = "DigitalGold";                 // Type
        sheet.Cell(2, 2).Value = "Digital Gold Wallet";          // Name
        sheet.Cell(2, 4).Value = "2026-01-01";                   // InvestmentStartDate
        sheet.Cell(2, 5).Value = 2000;                           // InvestedAmount
        sheet.Cell(2, 8).Value = 2100;                           // CurrentValue

        using var filled = new MemoryStream();
        wb.SaveAs(filled);
        filled.Position = 0;

        ImportResult result = _sut.ImportExcel(filled);

        Assert.Equal(1, result.ImportedCount);
        Assert.False(result.HasErrors);
        Assert.Equal("Digital Gold Wallet", _assets.GetAssets().Single().Name);
    }

    [Fact]
    public void CreateCsvTemplate_HasAllColumns()
    {
        string text = Encoding.UTF8.GetString(_sut.CreateCsvTemplate());
        foreach (string column in AssetImportColumns.All)
            Assert.Contains(column, text);
    }

    public void Dispose() => _db.Dispose();
}

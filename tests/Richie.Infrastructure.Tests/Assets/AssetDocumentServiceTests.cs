using System.IO;
using System.Text;
using Richie.Application.Assets;
using Richie.Domain.Assets;
using Richie.Infrastructure;
using Richie.Infrastructure.Assets;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Persistence;
using Richie.Infrastructure.Storage;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Assets;

public sealed class AssetDocumentServiceTests : IDisposable
{
    private sealed class TestKeyProvider : IDatabaseKeyProvider
    {
        private readonly string _key = Convert.ToBase64String(new byte[32]);
        public string GetOrCreateKey() => _key;
    }

    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new();
    private readonly UserSession _session = new();
    private readonly string _originalDataDir = AppPaths.DataDirectory;
    private readonly string _tempDataDir = Path.Combine(Path.GetTempPath(), $"richie-files-{Guid.NewGuid():N}");
    private readonly AssetDocumentService _sut;
    private readonly Guid _assetId;

    public AssetDocumentServiceTests()
    {
        AppPaths.DataDirectory = _tempDataDir;
        _session.SignIn(Guid.NewGuid(), "Tester");

        var assets = new AssetService(_db, new ValuationService(), _session, _clock);
        _assetId = assets.Create(new AssetInput
        {
            Type = AssetType.GoldJewellery,
            Name = "Necklace",
            InvestmentStartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            InvestedAmount = 1000,
            CurrentValue = 1000,
            InvestmentMode = InvestmentMode.LumpSum,
        });

        _sut = new AssetDocumentService(_db, new FileVault(new TestKeyProvider()), _session, _clock);
    }

    [Fact]
    public void Attach_StoresEncrypted_AndRoundTrips()
    {
        byte[] content = Encoding.UTF8.GetBytes("the original photo bytes");
        Guid id = _sut.Attach(_assetId, "necklace.png", content);

        AssetDocumentDto doc = Assert.Single(_sut.GetForAsset(_assetId));
        Assert.Equal("necklace.png", doc.FileName);
        Assert.Equal(DocumentKind.Image, doc.Kind);

        // On-disk bytes are encrypted (differ from the plaintext).
        string storedFile = Directory.GetFiles(Path.Combine(_tempDataDir, "photos", "assets"), "*.enc").Single();
        Assert.NotEqual(content, File.ReadAllBytes(storedFile));

        // OpenContent decrypts back to the original.
        Assert.Equal(content, _sut.OpenContent(id));
    }

    [Fact]
    public void Attach_DetectsPdfKind_AndStoresUnderDocuments()
    {
        _sut.Attach(_assetId, "certificate.pdf", Encoding.UTF8.GetBytes("%PDF-1.7 fake"));

        Assert.Equal(DocumentKind.Pdf, _sut.GetForAsset(_assetId).Single().Kind);
        Assert.True(Directory.Exists(Path.Combine(_tempDataDir, "documents", "assets")));
    }

    [Fact]
    public void Delete_RemovesRecordAndFile()
    {
        Guid id = _sut.Attach(_assetId, "photo.jpg", Encoding.UTF8.GetBytes("img"));
        string dir = Path.Combine(_tempDataDir, "photos", "assets");
        Assert.Single(Directory.GetFiles(dir, "*.enc"));

        Assert.True(_sut.Delete(id));
        Assert.Empty(_sut.GetForAsset(_assetId));
        Assert.Empty(Directory.GetFiles(dir, "*.enc"));
    }

    public void Dispose()
    {
        AppPaths.DataDirectory = _originalDataDir;
        _db.Dispose();
        if (Directory.Exists(_tempDataDir))
            Directory.Delete(_tempDataDir, recursive: true);
    }
}

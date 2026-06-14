using System.IO;
using System.Text;
using Richie.Application.Expenses;
using Richie.Domain.Assets;
using Richie.Domain.Expenses;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Expenses;
using Richie.Infrastructure.Persistence;
using Richie.Infrastructure.Storage;
using Richie.Infrastructure.Tests.Helpers;

namespace Richie.Infrastructure.Tests.Expenses;

public sealed class ExpenseDocumentServiceTests : IDisposable
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
    private readonly string _tempDataDir = Path.Combine(Path.GetTempPath(), $"richie-bills-{Guid.NewGuid():N}");
    private readonly ExpenseDocumentService _sut;
    private readonly Guid _expenseId;

    public ExpenseDocumentServiceTests()
    {
        AppPaths.DataDirectory = _tempDataDir;
        _session.SignIn(Guid.NewGuid(), "Tester");

        var expenses = new ExpenseService(_db, _session, _clock);
        _expenseId = expenses.Create(new ExpenseInput(_clock.UtcNow, 50m, ExpenseCategory.DiningRestaurants, "Me", "Lunch", null));
        _sut = new ExpenseDocumentService(_db, new FileVault(new TestKeyProvider()), _session, _clock);
    }

    [Fact]
    public void Attach_StoresEncrypted_AndRoundTrips()
    {
        byte[] content = Encoding.UTF8.GetBytes("scanned receipt bytes");
        Guid id = _sut.Attach(_expenseId, "receipt.jpg", content);

        ExpenseDocumentDto doc = Assert.Single(_sut.GetForExpense(_expenseId));
        Assert.Equal("receipt.jpg", doc.FileName);
        Assert.Equal(DocumentKind.Image, doc.Kind);

        string storedFile = Directory.GetFiles(Path.Combine(_tempDataDir, "receipts", "expenses"), "*.enc").Single();
        Assert.NotEqual(content, File.ReadAllBytes(storedFile));   // encrypted at rest
        Assert.Equal(content, _sut.OpenContent(id));               // decrypts back
    }

    [Fact]
    public void Delete_RemovesRecordAndFile()
    {
        Guid id = _sut.Attach(_expenseId, "bill.pdf", Encoding.UTF8.GetBytes("%PDF"));
        Assert.True(_sut.Delete(id));
        Assert.Empty(_sut.GetForExpense(_expenseId));
        Assert.Empty(Directory.GetFiles(Path.Combine(_tempDataDir, "receipts", "expenses"), "*.enc"));
    }

    public void Dispose()
    {
        AppPaths.DataDirectory = _originalDataDir;
        _db.Dispose();
        if (Directory.Exists(_tempDataDir))
            Directory.Delete(_tempDataDir, recursive: true);
    }
}

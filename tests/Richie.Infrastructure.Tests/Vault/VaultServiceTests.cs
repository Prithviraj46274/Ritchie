using Richie.Application.Vault;
using Richie.Domain.Vault;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Persistence;
using Richie.Infrastructure.Security;
using Richie.Infrastructure.Tests.Helpers;
using Richie.Infrastructure.Vault;

namespace Richie.Infrastructure.Tests.Vault;

public sealed class VaultServiceTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new();
    private readonly UserSession _session = new();
    private readonly VaultGate _gate;
    private readonly VaultService _sut;

    public VaultServiceTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _gate = new VaultGate(_db, _session, new Pbkdf2KeyDerivation(), new AesGcmFieldCipher(), _clock);
        _gate.SetupMasterPassword("master-pass-1");   // auto-unlocks
        _sut = new VaultService(_db, _session, _gate, _clock);
    }

    private static VaultEntryInput Input(string name = "HDFC Bank", string? password = "hunter2") =>
        new(name, "Bank", "https://hdfc.example", "alice", password, "primary account");

    [Fact]
    public void Create_StoresPasswordEncrypted_AndRevealReturnsPlaintext()
    {
        Guid id = _sut.Create(Input(password: "S3cr3t!"));

        // Stored cipher must not be the plaintext.
        using RichieDbContext db = _db.Create();
        VaultEntry stored = db.VaultEntries.Single(e => e.Id == id);
        Assert.NotEqual("S3cr3t!", stored.PasswordCipher);
        Assert.DoesNotContain("S3cr3t!", stored.PasswordCipher);

        // Reveal decrypts it back.
        Assert.Equal("S3cr3t!", _sut.RevealPassword(id));
    }

    [Fact]
    public void GetEntries_ReturnsSummariesWithoutPassword_AndSupportsSearch()
    {
        _sut.Create(Input("HDFC Bank"));
        _sut.Create(Input("Gmail"));

        Assert.Equal(2, _sut.GetEntries().Count);
        VaultEntrySummary match = Assert.Single(_sut.GetEntries(search: "gmail"));
        Assert.Equal("Gmail", match.AccountName);
    }

    [Fact]
    public void Update_WithBlankPassword_KeepsPasswordAndAge()
    {
        Guid id = _sut.Create(Input(password: "original"));
        DateTime originalAge = StoredAge(id);

        _clock.Advance(TimeSpan.FromDays(10));
        _sut.Update(id, new VaultEntryInput("HDFC Renamed", "Bank", null, "alice", Password: null, "note"));

        Assert.Equal("original", _sut.RevealPassword(id));
        Assert.Equal(originalAge, StoredAge(id));
        Assert.Equal("HDFC Renamed", _sut.GetById(id)!.AccountName);
    }

    [Fact]
    public void Update_WithNewPassword_ReencryptsAndBumpsAge()
    {
        Guid id = _sut.Create(Input(password: "original"));
        DateTime originalAge = StoredAge(id);

        _clock.Advance(TimeSpan.FromDays(10));
        _sut.Update(id, Input(password: "changed"));

        Assert.Equal("changed", _sut.RevealPassword(id));
        Assert.True(StoredAge(id) > originalAge);
    }

    [Fact]
    public void Operations_AreScopedToTheUser()
    {
        Guid id = _sut.Create(Input());

        _session.SignOut();
        _session.SignIn(Guid.NewGuid(), "Other");
        // The other user has no vault key; set one up so the gate is usable.
        _gate.SetupMasterPassword("other-master-pass");

        Assert.Empty(_sut.GetEntries());
        Assert.Null(_sut.GetById(id));
        Assert.False(_sut.Delete(id));
    }

    private DateTime StoredAge(Guid id)
    {
        using RichieDbContext db = _db.Create();
        return db.VaultEntries.Single(e => e.Id == id).PasswordUpdatedUtc;
    }

    public void Dispose() => _db.Dispose();
}

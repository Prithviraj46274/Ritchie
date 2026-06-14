using Richie.Application.Vault;
using Richie.Infrastructure.Authentication;
using Richie.Infrastructure.Persistence;
using Richie.Infrastructure.Security;
using Richie.Infrastructure.Tests.Helpers;
using Richie.Infrastructure.Vault;

namespace Richie.Infrastructure.Tests.Vault;

public sealed class VaultGateTests : IDisposable
{
    private readonly TempSqlCipherDatabase _db = new();
    private readonly FakeClock _clock = new();
    private readonly UserSession _session = new();
    private readonly VaultGate _gate;

    public VaultGateTests()
    {
        _session.SignIn(Guid.NewGuid(), "Tester");
        _gate = new VaultGate(_db, _session, new Pbkdf2KeyDerivation(), new AesGcmFieldCipher(), _clock);
    }

    [Fact]
    public void Setup_ThenUnlock_Succeeds_AndPersistsKey()
    {
        Assert.False(_gate.IsConfigured());

        VaultUnlockResult setup = _gate.SetupMasterPassword("master-pass-1");
        Assert.True(setup.IsSuccess);
        Assert.True(_gate.IsConfigured());
        Assert.True(_gate.IsUnlocked);   // auto-unlocks after setup

        _gate.Lock();
        Assert.False(_gate.IsUnlocked);

        VaultUnlockResult unlock = _gate.Unlock("master-pass-1");
        Assert.True(unlock.IsSuccess);
        Assert.True(_gate.IsUnlocked);
    }

    [Fact]
    public void Unlock_WithWrongPassword_Fails_AndStaysLocked()
    {
        _gate.SetupMasterPassword("master-pass-1");
        _gate.Lock();

        VaultUnlockResult result = _gate.Unlock("wrong-password");

        Assert.Equal(VaultUnlockStatus.IncorrectPassword, result.Status);
        Assert.False(_gate.IsUnlocked);
        Assert.False(_gate.Verify("wrong-password"));
        Assert.True(_gate.Verify("master-pass-1"));
    }

    [Fact]
    public void Setup_RejectsShortPassword_AndDoesNotConfigure()
    {
        VaultUnlockResult result = _gate.SetupMasterPassword("short");

        Assert.Equal(VaultUnlockStatus.ValidationFailed, result.Status);
        Assert.False(_gate.IsConfigured());
    }

    [Fact]
    public void EncryptDecrypt_RoundTrips_WhenUnlocked_AndThrowsWhenLocked()
    {
        _gate.SetupMasterPassword("master-pass-1");

        string cipher = _gate.Encrypt("hunter2");
        Assert.NotEqual("hunter2", cipher);
        Assert.Equal("hunter2", _gate.Decrypt(cipher));

        _gate.Lock();
        Assert.Throws<InvalidOperationException>(() => _gate.Decrypt(cipher));
        Assert.Throws<InvalidOperationException>(() => _gate.Encrypt("x"));
    }

    public void Dispose() => _db.Dispose();
}

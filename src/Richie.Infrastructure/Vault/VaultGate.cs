using System.Security.Cryptography;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Security;
using Richie.Application.Vault;
using Richie.Domain.Auditing;
using Richie.Domain.Vault;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Vault;

/// <summary>
/// Envelope-encryption gate for the vault. A random 256-bit DEK encrypts credentials; the DEK is
/// wrapped by a PBKDF2-derived KEK (master password + salt). Unwrapping is the password check —
/// AES-GCM authentication fails for a wrong password. The DEK lives in memory only while unlocked,
/// and only for the user it was unlocked for (a session change implicitly re-locks).
/// </summary>
public sealed class VaultGate : IVaultGate
{
    private const int DekLength = 32;     // 256-bit data-encryption key
    private const int SaltLength = 16;
    private const int Iterations = 100_000;
    private const int MinMasterPasswordLength = 8;

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IKeyDerivation _kdf;
    private readonly IFieldCipher _cipher;
    private readonly IClock _clock;

    private byte[]? _dek;
    private Guid? _unlockedFor;

    public VaultGate(
        IAppDbContextFactory factory, IUserSession session,
        IKeyDerivation kdf, IFieldCipher cipher, IClock clock)
    {
        _factory = factory;
        _session = session;
        _kdf = kdf;
        _cipher = cipher;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public bool IsUnlocked => _dek is not null && _unlockedFor == _session.UserId;

    public bool IsConfigured()
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.VaultKeys.Any(k => k.UserId == userId);
    }

    public VaultUnlockResult SetupMasterPassword(string masterPassword)
    {
        Guid userId = UserId;
        if (string.IsNullOrEmpty(masterPassword) || masterPassword.Length < MinMasterPasswordLength)
            return new VaultUnlockResult(VaultUnlockStatus.ValidationFailed,
                $"Master password must be at least {MinMasterPasswordLength} characters.");

        using RichieDbContext db = _factory.Create();
        if (db.VaultKeys.Any(k => k.UserId == userId))
            return new VaultUnlockResult(VaultUnlockStatus.ValidationFailed, "A vault master password is already set.");

        byte[] salt = _kdf.GenerateSalt(SaltLength);
        byte[] kek = _kdf.DeriveKey(masterPassword, salt, Iterations, DekLength);
        byte[] dek = RandomNumberGenerator.GetBytes(DekLength);
        string wrappedDek = _cipher.Encrypt(Convert.ToBase64String(dek), kek);

        DateTime now = _clock.UtcNow;
        db.VaultKeys.Add(new VaultKey
        {
            UserId = userId,
            Salt = salt,
            Iterations = Iterations,
            WrappedDek = wrappedDek,
            CreatedUtc = now
        });
        AuditWriter.Add(db, userId, now, "Vault", AuditAction.Create, nameof(VaultKey), userId,
            "Vault master password configured.");
        db.SaveChanges();

        _dek = dek;
        _unlockedFor = userId;
        return new VaultUnlockResult(VaultUnlockStatus.Success);
    }

    public VaultUnlockResult Unlock(string masterPassword)
    {
        if (TryUnwrap(masterPassword, out byte[]? dek))
        {
            _dek = dek;
            _unlockedFor = UserId;
            return new VaultUnlockResult(VaultUnlockStatus.Success);
        }
        return new VaultUnlockResult(VaultUnlockStatus.IncorrectPassword, "Incorrect master password.");
    }

    public bool Verify(string masterPassword) => TryUnwrap(masterPassword, out _);

    public void Lock()
    {
        if (_dek is not null)
            CryptographicOperations.ZeroMemory(_dek);
        _dek = null;
        _unlockedFor = null;
    }

    public string Encrypt(string plaintext) => _cipher.Encrypt(plaintext, RequireKey());

    public string Decrypt(string cipher) => _cipher.Decrypt(cipher, RequireKey());

    private byte[] RequireKey() =>
        IsUnlocked ? _dek! : throw new InvalidOperationException("The vault is locked.");

    private bool TryUnwrap(string masterPassword, out byte[]? dek)
    {
        dek = null;
        Guid userId = UserId;

        using RichieDbContext db = _factory.Create();
        VaultKey? key = db.VaultKeys.FirstOrDefault(k => k.UserId == userId);
        if (key is null)
            return false;

        byte[] kek = _kdf.DeriveKey(masterPassword, key.Salt, key.Iterations, DekLength);
        try
        {
            dek = Convert.FromBase64String(_cipher.Decrypt(key.WrappedDek, kek));
            return true;
        }
        catch (CryptographicException)
        {
            // Wrong password → KEK is wrong → GCM tag verification fails.
            return false;
        }
    }
}

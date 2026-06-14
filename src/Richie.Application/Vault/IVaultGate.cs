namespace Richie.Application.Vault;

/// <summary>
/// Owns the vault's encryption key lifecycle and enforces the re-authentication gate (PRD §8.1):
/// the data-encryption key is held in memory only while the vault is unlocked, and is dropped on
/// <see cref="Lock"/> (called on every navigation away / logout / auto-lock). All field
/// encryption/decryption flows through here so a credential can never be read while locked.
/// Scoped to the currently signed-in user.
/// </summary>
public interface IVaultGate
{
    /// <summary>True once the current user has established a vault master password.</summary>
    bool IsConfigured();

    /// <summary>True while the vault is unlocked for the current user (key held in memory).</summary>
    bool IsUnlocked { get; }

    /// <summary>First-time setup: choose the vault master password. Auto-unlocks on success.</summary>
    VaultUnlockResult SetupMasterPassword(string masterPassword);

    /// <summary>Verify the master password and unlock the vault (holds the key for this access).</summary>
    VaultUnlockResult Unlock(string masterPassword);

    /// <summary>Verify the master password without changing lock state (for reveal/export re-auth).</summary>
    bool Verify(string masterPassword);

    /// <summary>Drop the in-memory key — the next access must re-authenticate.</summary>
    void Lock();

    /// <summary>Encrypt a field value with the unlocked vault key. Throws if locked.</summary>
    string Encrypt(string plaintext);

    /// <summary>Decrypt a field value with the unlocked vault key. Throws if locked.</summary>
    string Decrypt(string cipher);
}

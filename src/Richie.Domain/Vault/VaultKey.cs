namespace Richie.Domain.Vault;

/// <summary>
/// Per-user envelope key material for the vault. A random 256-bit data-encryption key (DEK)
/// encrypts every credential; the DEK itself is wrapped (AES-256-GCM) by a key-encryption key
/// (KEK) derived via PBKDF2 from the user's vault master password + <see cref="Salt"/>. Only the
/// wrapped DEK is stored — the KEK and DEK exist in memory only while the vault is unlocked.
/// The GCM authentication tag on <see cref="WrappedDek"/> doubles as the master-password verifier:
/// unwrap succeeds only when the supplied password derives the correct KEK.
/// </summary>
public class VaultKey
{
    /// <summary>Primary key — one record per user.</summary>
    public Guid UserId { get; set; }

    /// <summary>PBKDF2 salt for deriving the KEK from the master password.</summary>
    public byte[] Salt { get; set; } = [];

    /// <summary>PBKDF2 iteration count used to derive the KEK (stored for forward-compatibility).</summary>
    public int Iterations { get; set; }

    /// <summary>The DEK encrypted under the KEK (AES-256-GCM base64 payload).</summary>
    public string WrappedDek { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}

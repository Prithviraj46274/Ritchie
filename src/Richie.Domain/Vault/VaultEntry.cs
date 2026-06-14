namespace Richie.Domain.Vault;

/// <summary>
/// A stored credential (PRD §8). The password is held only as an AES-256-GCM ciphertext
/// (<see cref="PasswordCipher"/>) produced with the user's vault data-encryption key — it is
/// never persisted in plaintext. All other fields are stored as entered (the whole DB file is
/// itself SQLCipher-encrypted).
/// </summary>
public class VaultEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Account / platform name (e.g. "HDFC Bank").</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Free-text category / asset type (e.g. "Bank", "Mutual Fund"). Optional.</summary>
    public string? Category { get; set; }

    /// <summary>Login URL. Optional.</summary>
    public string? Url { get; set; }

    /// <summary>The account's user id / email. Stored as entered.</summary>
    public string? LoginId { get; set; }

    /// <summary>AES-256-GCM payload of the password (self-describing base64: nonce|tag|ciphertext).</summary>
    public string PasswordCipher { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    /// <summary>When the password value last changed — drives password-age health checks (PRD §8.6).</summary>
    public DateTime PasswordUpdatedUtc { get; set; }
}

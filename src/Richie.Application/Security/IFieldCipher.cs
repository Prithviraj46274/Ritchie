namespace Richie.Application.Security;

/// <summary>
/// Authenticated per-field encryption (AES-256-GCM) applied to sensitive values
/// (e.g. vault passwords) before any database write. Returns/accepts a self-describing
/// base64 payload that packs the nonce, authentication tag, and ciphertext together.
/// </summary>
public interface IFieldCipher
{
    string Encrypt(string plaintext, byte[] key);
    string Decrypt(string payload, byte[] key);
}

using System.Security.Cryptography;
using System.Text;
using Richie.Application.Security;

namespace Richie.Infrastructure.Security;

/// <summary>
/// AES-256-GCM authenticated field encryption. Payload layout (then base64-encoded):
/// [nonce (12 bytes)][tag (16 bytes)][ciphertext]. Decrypt fails (throws) if the
/// payload, tag, or key has been tampered with.
/// </summary>
public sealed class AesGcmFieldCipher : IFieldCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Encrypt(string plaintext, byte[] key)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        byte[] payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);
        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string payload, byte[] key)
    {
        byte[] bytes = Convert.FromBase64String(payload);
        ReadOnlySpan<byte> nonce = bytes.AsSpan(0, NonceSize);
        ReadOnlySpan<byte> tag = bytes.AsSpan(NonceSize, TagSize);
        ReadOnlySpan<byte> ciphertext = bytes.AsSpan(NonceSize + TagSize);

        byte[] plainBytes = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}

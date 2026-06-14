using System.IO;
using System.Security.Cryptography;
using Richie.Application.Storage;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Storage;

/// <summary>
/// AES-256-GCM file encryption at rest, keyed by the DPAPI-protected database key. On-disk layout
/// per stored file: [nonce(12)][tag(16)][ciphertext].
/// </summary>
public sealed class FileVault : IFileVault
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IDatabaseKeyProvider _keyProvider;

    public FileVault(IDatabaseKeyProvider keyProvider) => _keyProvider = keyProvider;

    private byte[] Key() => Convert.FromBase64String(_keyProvider.GetOrCreateKey());

    public string Save(byte[] content, string subfolder)
    {
        string directory = Path.Combine(AppPaths.DataDirectory, subfolder);
        Directory.CreateDirectory(directory);

        string storedFileName = $"{Guid.NewGuid():N}.enc";
        File.WriteAllBytes(Path.Combine(directory, storedFileName), Encrypt(content, Key()));
        return storedFileName;
    }

    public byte[] Read(string subfolder, string storedFileName)
    {
        byte[] payload = File.ReadAllBytes(Path.Combine(AppPaths.DataDirectory, subfolder, storedFileName));
        return Decrypt(payload, Key());
    }

    public void Delete(string subfolder, string storedFileName)
    {
        string path = Path.Combine(AppPaths.DataDirectory, subfolder, storedFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static byte[] Encrypt(byte[] content, byte[] key)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[content.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, content, ciphertext, tag);

        byte[] payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);
        return payload;
    }

    private static byte[] Decrypt(byte[] payload, byte[] key)
    {
        ReadOnlySpan<byte> nonce = payload.AsSpan(0, NonceSize);
        ReadOnlySpan<byte> tag = payload.AsSpan(NonceSize, TagSize);
        ReadOnlySpan<byte> ciphertext = payload.AsSpan(NonceSize + TagSize);

        byte[] content = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, content);
        return content;
    }
}

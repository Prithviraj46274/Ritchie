using System.Security.Cryptography;
using Richie.Application.Security;

namespace Richie.Infrastructure.Security;

/// <summary>
/// PBKDF2 (SHA-256) key derivation from the master password.
/// </summary>
public sealed class Pbkdf2KeyDerivation : IKeyDerivation
{
    public byte[] DeriveKey(string password, byte[] salt, int iterations, int keyByteLength) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keyByteLength);

    public byte[] GenerateSalt(int byteLength = 16) =>
        RandomNumberGenerator.GetBytes(byteLength);
}

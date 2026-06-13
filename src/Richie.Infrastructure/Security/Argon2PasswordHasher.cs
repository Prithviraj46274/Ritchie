using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Richie.Application.Security;

namespace Richie.Infrastructure.Security;

/// <summary>
/// Argon2id password hasher. Parameters follow OWASP guidance (m=19 MiB, t=2, p=1).
/// Encoded format: argon2id$m$t$p$saltBase64$hashBase64 — self-contained for verification.
/// </summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemoryKiB = 19456; // 19 MiB
    private const int Iterations = 2;
    private const int Parallelism = 1;

    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Compute(password, salt, MemoryKiB, Iterations, Parallelism, HashSize);
        return string.Join('$',
            "argon2id", MemoryKiB, Iterations, Parallelism,
            Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string encodedHash)
    {
        string[] parts = encodedHash.Split('$');
        if (parts.Length != 6 || parts[0] != "argon2id")
            return false;

        if (!int.TryParse(parts[1], out int memoryKiB) ||
            !int.TryParse(parts[2], out int iterations) ||
            !int.TryParse(parts[3], out int parallelism))
            return false;

        byte[] salt = Convert.FromBase64String(parts[4]);
        byte[] expected = Convert.FromBase64String(parts[5]);
        byte[] actual = Compute(password, salt, memoryKiB, iterations, parallelism, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Compute(string password, byte[] salt, int memoryKiB, int iterations, int parallelism, int hashSize)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memoryKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };
        return argon2.GetBytes(hashSize);
    }
}

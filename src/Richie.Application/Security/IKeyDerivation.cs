namespace Richie.Application.Security;

/// <summary>
/// Derives cryptographic keys from the master password (PBKDF2) and generates salts.
/// </summary>
public interface IKeyDerivation
{
    byte[] DeriveKey(string password, byte[] salt, int iterations, int keyByteLength);
    byte[] GenerateSalt(int byteLength = 16);
}

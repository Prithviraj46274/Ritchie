using System.Security.Cryptography;
using Richie.Infrastructure.Security;

namespace Richie.Infrastructure.Tests.Security;

public class AesGcmFieldCipherTests
{
    private readonly AesGcmFieldCipher _cipher = new();
    private static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

    [Fact]
    public void Decrypt_RoundTrips_Plaintext()
    {
        byte[] key = NewKey();
        const string secret = "P@ssw0rd-with-üñïçödé!";

        string payload = _cipher.Encrypt(secret, key);
        string recovered = _cipher.Decrypt(payload, key);

        Assert.Equal(secret, recovered);
        Assert.NotEqual(secret, payload);
    }

    [Fact]
    public void Encrypt_ProducesDifferentPayloads_ForSamePlaintext()
    {
        byte[] key = NewKey();

        string first = _cipher.Encrypt("same", key);
        string second = _cipher.Encrypt("same", key);

        // Random nonce per call => different payloads even for identical input.
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        string payload = _cipher.Encrypt("secret", NewKey());

        Assert.Throws<AuthenticationTagMismatchException>(() => _cipher.Decrypt(payload, NewKey()));
    }

    [Fact]
    public void Decrypt_TamperedPayload_Throws()
    {
        byte[] key = NewKey();
        string payload = _cipher.Encrypt("secret", key);

        byte[] bytes = Convert.FromBase64String(payload);
        bytes[^1] ^= 0xFF; // flip a ciphertext bit
        string tampered = Convert.ToBase64String(bytes);

        Assert.Throws<AuthenticationTagMismatchException>(() => _cipher.Decrypt(tampered, key));
    }
}

using Richie.Infrastructure.Security;

namespace Richie.Infrastructure.Tests.Security;

public class Pbkdf2KeyDerivationTests
{
    private readonly Pbkdf2KeyDerivation _kdf = new();

    [Fact]
    public void DeriveKey_IsDeterministic_ForSameInputs()
    {
        byte[] salt = new byte[16];

        byte[] a = _kdf.DeriveKey("master-pass", salt, iterations: 100_000, keyByteLength: 32);
        byte[] b = _kdf.DeriveKey("master-pass", salt, iterations: 100_000, keyByteLength: 32);

        Assert.Equal(32, a.Length);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DeriveKey_DiffersByPassword_AndBySalt()
    {
        byte[] salt1 = _kdf.GenerateSalt();
        byte[] salt2 = _kdf.GenerateSalt();

        byte[] key = _kdf.DeriveKey("master-pass", salt1, 100_000, 32);
        byte[] differentPassword = _kdf.DeriveKey("other-pass", salt1, 100_000, 32);
        byte[] differentSalt = _kdf.DeriveKey("master-pass", salt2, 100_000, 32);

        Assert.NotEqual(key, differentPassword);
        Assert.NotEqual(key, differentSalt);
    }

    [Fact]
    public void GenerateSalt_ReturnsRequestedLength_AndIsRandom()
    {
        byte[] a = _kdf.GenerateSalt();
        byte[] b = _kdf.GenerateSalt();

        Assert.Equal(16, a.Length);
        Assert.NotEqual(a, b);
    }
}

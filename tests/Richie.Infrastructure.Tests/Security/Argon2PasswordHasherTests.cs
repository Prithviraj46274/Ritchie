using Richie.Infrastructure.Security;

namespace Richie.Infrastructure.Tests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        string hash = _hasher.Hash("correct horse battery staple");

        Assert.True(_hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        string hash = _hasher.Hash("correct horse battery staple");

        Assert.False(_hasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Hash_ProducesDifferentHashes_ForSamePassword()
    {
        // Random salt per call => different encoded hashes that both still verify.
        string a = _hasher.Hash("same-password");
        string b = _hasher.Hash("same-password");

        Assert.NotEqual(a, b);
        Assert.True(_hasher.Verify("same-password", a));
        Assert.True(_hasher.Verify("same-password", b));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash()
    {
        Assert.False(_hasher.Verify("anything", "not-a-valid-hash"));
    }
}

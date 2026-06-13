using System.Text;
using Richie.Infrastructure.Security;

namespace Richie.Infrastructure.Tests.Security;

public class DpapiKeyProtectorTests
{
    private readonly DpapiKeyProtector _protector = new();

    [Fact]
    public void Unprotect_RoundTrips_OriginalBytes()
    {
        byte[] secret = Encoding.UTF8.GetBytes("the-database-key-material");

        byte[] protectedBytes = _protector.Protect(secret);
        byte[] recovered = _protector.Unprotect(protectedBytes);

        Assert.Equal(secret, recovered);
    }

    [Fact]
    public void Protect_DoesNotReturnPlaintext()
    {
        byte[] secret = Encoding.UTF8.GetBytes("the-database-key-material");

        byte[] protectedBytes = _protector.Protect(secret);

        Assert.NotEqual(secret, protectedBytes);
        Assert.True(protectedBytes.Length > secret.Length);
    }
}

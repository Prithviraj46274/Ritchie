using System.Security.Cryptography;
using Richie.Application.Security;

namespace Richie.Infrastructure.Security;

/// <summary>
/// DPAPI-backed key protector. Wrapped data is bound to the current Windows user
/// account and cannot be unwrapped by another user or on another machine.
/// </summary>
public sealed class DpapiKeyProtector : IKeyProtector
{
    public byte[] Protect(byte[] plaintext) =>
        ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);

    public byte[] Unprotect(byte[] protectedData) =>
        ProtectedData.Unprotect(protectedData, optionalEntropy: null, DataProtectionScope.CurrentUser);
}

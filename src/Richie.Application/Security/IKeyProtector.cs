namespace Richie.Application.Security;

/// <summary>
/// Protects key material at rest using a mechanism tied to the current Windows user
/// (DPAPI). Used to wrap the database/master key so it is unreadable by other accounts.
/// </summary>
public interface IKeyProtector
{
    byte[] Protect(byte[] plaintext);
    byte[] Unprotect(byte[] protectedData);
}

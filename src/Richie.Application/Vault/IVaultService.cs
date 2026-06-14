namespace Richie.Application.Vault;

/// <summary>Fields for creating/editing a credential. On update, a null/empty
/// <see cref="Password"/> means "keep the existing password" (so other fields can be edited
/// without revealing or re-typing it).</summary>
public sealed record VaultEntryInput(
    string AccountName,
    string? Category,
    string? Url,
    string? LoginId,
    string? Password,
    string? Notes);

/// <summary>List/grid row — never carries the password (shown masked in the UI).</summary>
public sealed record VaultEntrySummary(
    Guid Id,
    string AccountName,
    string? Category,
    string? Url,
    string? LoginId);

/// <summary>Edit prefill — metadata only, no password.</summary>
public sealed record VaultEntryDetail(
    Guid Id,
    string AccountName,
    string? Category,
    string? Url,
    string? LoginId,
    string? Notes);

/// <summary>
/// CRUD over vault credentials for the signed-in user. Every password is encrypted via
/// <see cref="IVaultGate"/> before any DB write; the vault must be unlocked for all operations.
/// All writes are audited.
/// </summary>
public interface IVaultService
{
    IReadOnlyList<VaultEntrySummary> GetEntries(string? search = null, string? category = null);

    /// <summary>Distinct, non-empty categories in use — powers the category filter.</summary>
    IReadOnlyList<string> GetCategories();

    VaultEntryDetail? GetById(Guid id);

    /// <summary>Decrypt and return the password — gate must be unlocked (reveal re-auth is a UI concern).</summary>
    string? RevealPassword(Guid id);

    Guid Create(VaultEntryInput input);

    bool Update(Guid id, VaultEntryInput input);

    bool Delete(Guid id);
}

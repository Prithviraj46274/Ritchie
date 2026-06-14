using Microsoft.EntityFrameworkCore;
using Richie.Application.Abstractions;
using Richie.Application.Authentication;
using Richie.Application.Vault;
using Richie.Domain.Auditing;
using Richie.Domain.Vault;
using Richie.Infrastructure.Auditing;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Vault;

public sealed class VaultService : IVaultService
{
    private const string Module = "Vault";

    private readonly IAppDbContextFactory _factory;
    private readonly IUserSession _session;
    private readonly IVaultGate _gate;
    private readonly IClock _clock;

    public VaultService(IAppDbContextFactory factory, IUserSession session, IVaultGate gate, IClock clock)
    {
        _factory = factory;
        _session = session;
        _gate = gate;
        _clock = clock;
    }

    private Guid UserId => _session.UserId ?? throw new InvalidOperationException("No authenticated user.");

    public IReadOnlyList<VaultEntrySummary> GetEntries(string? search = null, string? category = null)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        List<VaultEntry> list = db.VaultEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.AccountName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(category))
            list = list.Where(e => e.Category == category).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            list = list.Where(e =>
                Contains(e.AccountName, term) || Contains(e.LoginId, term) || Contains(e.Url, term)).ToList();
        }

        return list.Select(e => new VaultEntrySummary(e.Id, e.AccountName, e.Category, e.Url, e.LoginId)).ToList();
    }

    public IReadOnlyList<string> GetCategories()
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        return db.VaultEntries.AsNoTracking()
            .Where(e => e.UserId == userId && e.Category != null && e.Category != "")
            .Select(e => e.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    public VaultEntryDetail? GetById(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        VaultEntry? e = db.VaultEntries.AsNoTracking().FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return e is null ? null : new VaultEntryDetail(e.Id, e.AccountName, e.Category, e.Url, e.LoginId, e.Notes);
    }

    public string? RevealPassword(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        VaultEntry? e = db.VaultEntries.AsNoTracking().FirstOrDefault(x => x.Id == id && x.UserId == userId);
        return e is null ? null : _gate.Decrypt(e.PasswordCipher);
    }

    public Guid Create(VaultEntryInput input)
    {
        Guid userId = UserId;
        if (string.IsNullOrWhiteSpace(input.Password))
            throw new ArgumentException("A password is required.", nameof(input));

        DateTime now = _clock.UtcNow;
        var entry = new VaultEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountName = input.AccountName.Trim(),
            Category = Trim(input.Category),
            Url = Trim(input.Url),
            LoginId = Trim(input.LoginId),
            PasswordCipher = _gate.Encrypt(input.Password),
            Notes = Trim(input.Notes),
            CreatedUtc = now,
            UpdatedUtc = now,
            PasswordUpdatedUtc = now
        };

        using RichieDbContext db = _factory.Create();
        db.VaultEntries.Add(entry);
        AuditWriter.Add(db, userId, now, Module, AuditAction.Create, nameof(VaultEntry), entry.Id,
            $"Added vault entry '{entry.AccountName}'.");
        db.SaveChanges();
        return entry.Id;
    }

    public bool Update(Guid id, VaultEntryInput input)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        VaultEntry? e = db.VaultEntries.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (e is null)
            return false;

        DateTime now = _clock.UtcNow;
        e.AccountName = input.AccountName.Trim();
        e.Category = Trim(input.Category);
        e.Url = Trim(input.Url);
        e.LoginId = Trim(input.LoginId);
        e.Notes = Trim(input.Notes);
        e.UpdatedUtc = now;

        // A blank password means "keep current"; only re-encrypt + bump age when it actually changes.
        if (!string.IsNullOrWhiteSpace(input.Password) && _gate.Decrypt(e.PasswordCipher) != input.Password)
        {
            e.PasswordCipher = _gate.Encrypt(input.Password);
            e.PasswordUpdatedUtc = now;
        }

        AuditWriter.Add(db, userId, now, Module, AuditAction.Update, nameof(VaultEntry), e.Id,
            $"Updated vault entry '{e.AccountName}'.");
        db.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        Guid userId = UserId;
        using RichieDbContext db = _factory.Create();
        VaultEntry? e = db.VaultEntries.FirstOrDefault(x => x.Id == id && x.UserId == userId);
        if (e is null)
            return false;

        db.VaultEntries.Remove(e);
        AuditWriter.Add(db, userId, _clock.UtcNow, Module, AuditAction.Delete, nameof(VaultEntry), e.Id,
            $"Deleted vault entry '{e.AccountName}'.");
        db.SaveChanges();
        return true;
    }

    private static bool Contains(string? value, string term) =>
        value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

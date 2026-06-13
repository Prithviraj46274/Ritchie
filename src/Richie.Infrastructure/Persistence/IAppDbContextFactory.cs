namespace Richie.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="RichieDbContext"/> bound to a SQLCipher-encrypted database file.
/// The <paramref name="key"/> is the session key established after login; without the
/// correct key the file cannot be opened.
/// </summary>
public interface IAppDbContextFactory
{
    RichieDbContext Create(string databasePath, string key);
}

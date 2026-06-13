using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Richie.Infrastructure.Persistence;

/// <summary>
/// Builds <see cref="RichieDbContext"/> instances over a SQLCipher-encrypted SQLite file.
/// The connection-string Password issues <c>PRAGMA key</c>, which the bundled SQLCipher
/// native provider uses to encrypt/decrypt the whole file with AES-256.
/// </summary>
public sealed class SqlCipherDbContextFactory : IAppDbContextFactory
{
    static SqlCipherDbContextFactory()
    {
        // Register the SQLCipher native provider (bundle_e_sqlcipher) for SQLitePCLRaw.
        SQLitePCL.Batteries_V2.Init();
    }

    public RichieDbContext Create(string databasePath, string key)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Password = key
        }.ToString();

        DbContextOptions<RichieDbContext> options = new DbContextOptionsBuilder<RichieDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new RichieDbContext(options);
    }
}

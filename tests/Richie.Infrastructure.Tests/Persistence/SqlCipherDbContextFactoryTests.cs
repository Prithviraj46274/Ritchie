using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Richie.Infrastructure.Persistence;

namespace Richie.Infrastructure.Tests.Persistence;

/// <summary>
/// Validates the Phase-0 risk item: SQLCipher (bundle_e_sqlcipher) on .NET 10 actually
/// encrypts the file and gates access on the key, through the EF Core stack.
/// </summary>
public sealed class SqlCipherDbContextFactoryTests : IDisposable
{
    private const string CorrectKey = "correct-master-key";
    private readonly SqlCipherDbContextFactory _factory = new();
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"richie-test-{Guid.NewGuid():N}.db");

    private void Seed(string key)
    {
        using RichieDbContext ctx = _factory.Create(_dbPath, key);
        ctx.Database.ExecuteSqlRaw("CREATE TABLE probe(id INTEGER PRIMARY KEY, val TEXT);");
        ctx.Database.ExecuteSqlRaw("INSERT INTO probe(val) VALUES ('hello-encrypted-world');");
    }

    [Fact]
    public void Database_OpensAndReads_WithCorrectKey()
    {
        Seed(CorrectKey);

        using RichieDbContext ctx = _factory.Create(_dbPath, CorrectKey);
        ctx.Database.OpenConnection();
        using var cmd = ctx.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT val FROM probe LIMIT 1;";

        Assert.Equal("hello-encrypted-world", cmd.ExecuteScalar());
    }

    [Fact]
    public void Database_FailsToOpen_WithWrongKey()
    {
        Seed(CorrectKey);

        using RichieDbContext ctx = _factory.Create(_dbPath, "wrong-key");

        // SQLCipher cannot decrypt the header with the wrong key.
        Assert.Throws<SqliteException>(() =>
        {
            ctx.Database.OpenConnection();
            using var cmd = ctx.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT val FROM probe LIMIT 1;";
            cmd.ExecuteScalar();
        });
    }

    [Fact]
    public void FileOnDisk_IsNotPlaintextSqlite()
    {
        Seed(CorrectKey);
        SqliteConnection.ClearAllPools(); // release the pooled file handle so we can read the file

        byte[] header = new byte[16];
        using (FileStream fs = File.OpenRead(_dbPath))
        {
            int read = fs.Read(header, 0, header.Length);
            Assert.Equal(16, read);
        }

        // An unencrypted SQLite file begins with "SQLite format 3\0".
        string asText = Encoding.ASCII.GetString(header);
        Assert.DoesNotContain("SQLite format 3", asText);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

using Microsoft.EntityFrameworkCore;

namespace Richie.Infrastructure.Persistence;

/// <summary>
/// The application's EF Core context. Entities are added per phase as modules land;
/// in Phase 0 it exists to prove the SQLCipher-encrypted connection works end-to-end.
/// </summary>
public class RichieDbContext : DbContext
{
    public RichieDbContext(DbContextOptions<RichieDbContext> options) : base(options)
    {
    }
}

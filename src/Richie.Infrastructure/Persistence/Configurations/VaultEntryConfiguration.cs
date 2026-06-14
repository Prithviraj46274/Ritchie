using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Vault;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class VaultEntryConfiguration : IEntityTypeConfiguration<VaultEntry>
{
    public void Configure(EntityTypeBuilder<VaultEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.AccountName });
        builder.Property(e => e.AccountName).HasMaxLength(200);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.Url).HasMaxLength(500);
        builder.Property(e => e.LoginId).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);
    }
}

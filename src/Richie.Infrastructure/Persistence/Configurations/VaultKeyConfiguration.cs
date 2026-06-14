using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Vault;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class VaultKeyConfiguration : IEntityTypeConfiguration<VaultKey>
{
    public void Configure(EntityTypeBuilder<VaultKey> builder)
    {
        builder.HasKey(k => k.UserId);
        builder.Property(k => k.WrappedDek).HasMaxLength(200);
    }
}

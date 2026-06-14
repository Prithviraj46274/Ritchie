using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Assets;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class AssetDocumentConfiguration : IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.AssetId);
        builder.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(260);
        builder.Property(d => d.StoredFileName).IsRequired().HasMaxLength(80);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

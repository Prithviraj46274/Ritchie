using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Assets;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasKey(g => g.Id);
        builder.HasIndex(g => g.UserId);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(120);
    }
}

public sealed class AssetGoalLinkConfiguration : IEntityTypeConfiguration<AssetGoalLink>
{
    public void Configure(EntityTypeBuilder<AssetGoalLink> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.GoalId, l.AssetId }).IsUnique();

        builder.HasOne<Goal>()
            .WithMany()
            .HasForeignKey(l => l.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(l => l.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

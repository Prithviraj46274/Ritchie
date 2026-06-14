using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IncomeEntity = Richie.Domain.Income.Income;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class IncomeConfiguration : IEntityTypeConfiguration<IncomeEntity>
{
    public void Configure(EntityTypeBuilder<IncomeEntity> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.UserId, i.Date });
        builder.Property(i => i.Source).HasMaxLength(120);
        builder.Property(i => i.Notes).HasMaxLength(2000);
    }
}

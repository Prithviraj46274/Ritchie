using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Expenses;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class ExpenseRecurringConfiguration : IEntityTypeConfiguration<ExpenseRecurring>
{
    public void Configure(EntityTypeBuilder<ExpenseRecurring> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.IsEnabled, r.NextRunDateUtc });
        builder.Property(r => r.SpentBy).HasMaxLength(120);
        builder.Property(r => r.SpentFor).HasMaxLength(200);
        builder.Property(r => r.Notes).HasMaxLength(2000);
    }
}

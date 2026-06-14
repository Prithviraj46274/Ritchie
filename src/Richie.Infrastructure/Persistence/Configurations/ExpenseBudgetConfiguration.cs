using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Expenses;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class ExpenseBudgetConfiguration : IEntityTypeConfiguration<ExpenseBudget>
{
    public void Configure(EntityTypeBuilder<ExpenseBudget> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.UserId, b.Category }).IsUnique();
    }
}

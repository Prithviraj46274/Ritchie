using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Expenses;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class ExpenseDocumentConfiguration : IEntityTypeConfiguration<ExpenseDocument>
{
    public void Configure(EntityTypeBuilder<ExpenseDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.UserId, d.ExpenseId });
        builder.Property(d => d.OriginalFileName).HasMaxLength(260);
        builder.Property(d => d.StoredFileName).HasMaxLength(100);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Richie.Domain.Liabilities;

namespace Richie.Infrastructure.Persistence.Configurations;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.UserId, l.NextDueDate });
        builder.HasIndex(l => new { l.UserId, l.Status });
        builder.Property(l => l.Provider).HasMaxLength(150);
        builder.Property(l => l.AccountNumber).HasMaxLength(100);
        builder.Property(l => l.BorrowerName).HasMaxLength(150);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.InterestType).HasMaxLength(20);
        builder.Property(l => l.CoApplicant).HasMaxLength(150);
        builder.Property(l => l.CollateralType).HasMaxLength(150);

        builder.HasMany(l => l.Payments)
            .WithOne(p => p.Loan)
            .HasForeignKey(p => p.LoanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LoanPaymentConfiguration : IEntityTypeConfiguration<LoanPayment>
{
    public void Configure(EntityTypeBuilder<LoanPayment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.LoanId, p.PaymentDate });
        builder.Property(p => p.Note).HasMaxLength(500);
    }
}
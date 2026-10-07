using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration
    : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ProviderTransactionId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_Payments_Amount_Positive",
            "\"Amount\" > 0");

        builder.HasOne(x => x.Transaction)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TransactionId,
            x.CreatedAt
        });
    }
}
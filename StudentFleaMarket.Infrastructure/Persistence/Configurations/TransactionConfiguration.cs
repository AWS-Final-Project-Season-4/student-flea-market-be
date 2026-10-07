using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration
    : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_Transactions_TotalAmount_NonNegative",
            "\"TotalAmount\" >= 0");

        builder.HasCheckConstraint(
            "CK_Transactions_DifferentUsers",
            "\"InitiatorId\" <> \"CounterpartyId\"");

        builder.HasOne(x => x.Initiator)
            .WithMany()
            .HasForeignKey(x => x.InitiatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Counterparty)
            .WithMany()
            .HasForeignKey(x => x.CounterpartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ExchangeRequest)
            .WithOne()
            .HasForeignKey<Transaction>(x => x.ExchangeRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ExchangeRequestId)
            .IsUnique()
            .HasFilter("\"ExchangeRequestId\" IS NOT NULL");

        builder.HasIndex(x => new
        {
            x.InitiatorId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.CounterpartyId,
            x.CreatedAt
        });
    }
}
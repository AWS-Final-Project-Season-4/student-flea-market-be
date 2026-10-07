using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence.Configurations;

public class ExchangeRequestConfiguration
    : IEntityTypeConfiguration<ExchangeRequest>
{
    public void Configure(EntityTypeBuilder<ExchangeRequest> builder)
    {
        builder.ToTable("ExchangeRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdditionalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_ExchangeRequests_AdditionalAmount_NonNegative",
            "\"AdditionalAmount\" >= 0");

        builder.HasCheckConstraint(
            "CK_ExchangeRequests_OfferExists",
            "\"OfferedItemId\" IS NOT NULL OR \"AdditionalAmount\" > 0");

        builder.HasCheckConstraint(
            "CK_ExchangeRequests_DifferentItems",
            "\"OfferedItemId\" IS NULL OR \"OfferedItemId\" <> \"RequestedItemId\"");

        builder.HasOne(x => x.Requester)
            .WithMany()
            .HasForeignKey(x => x.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestedItem)
            .WithMany()
            .HasForeignKey(x => x.RequestedItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OfferedItem)
            .WithMany()
            .HasForeignKey(x => x.OfferedItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.RequestedItemId,
            x.Status
        });

        builder.HasIndex(x => new
        {
            x.RequesterId,
            x.Status
        });

        builder.HasIndex(x => new
        {
            x.Status,
            x.ExpiresAt
        });
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

public class ItemConfiguration
    : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(5000);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Condition)
            .IsRequired();

        builder.Property(x => x.IsExchangeEnabled)
            .HasDefaultValue(false);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_Items_Price_NonNegative",
            "\"Price\" >= 0"
        );

        // Seller → Item
        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category → Item
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.SellerId,
            x.Status
        });

        builder.HasIndex(x => new
        {
            x.CategoryId,
            x.Status
        });

        builder.HasIndex(x => new
        {
            x.Status,
            x.CreatedAt
        });
    }
}

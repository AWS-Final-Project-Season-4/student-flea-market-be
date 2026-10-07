using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence.Configurations;

public class TransactionItemConfiguration
    : IEntityTypeConfiguration<TransactionItem>
{
    public void Configure(EntityTypeBuilder<TransactionItem> builder)
    {
        builder.ToTable("TransactionItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .IsRequired();

        builder.HasOne(x => x.Transaction)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TransactionId);

        builder.HasIndex(x => x.ItemId);
    }
}
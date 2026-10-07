using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration
    : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Rating)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(2000);

        builder.HasCheckConstraint(
            "CK_Reviews_Rating_Range",
            "\"Rating\" >= 1 AND \"Rating\" <= 5");

        builder.HasCheckConstraint(
            "CK_Reviews_DifferentUsers",
            "\"ReviewerId\" <> \"RevieweeId\"");

        builder.HasOne(x => x.Transaction)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Reviewer)
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Reviewee)
            .WithMany()
            .HasForeignKey(x => x.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TransactionId,
            x.ReviewerId
        }).IsUnique();

        builder.HasIndex(x => x.RevieweeId);

        builder.HasIndex(x => x.ItemId);
    }
}
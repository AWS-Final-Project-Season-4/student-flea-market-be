using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence.Configurations;

public class ReportConfiguration
    : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.AdminNote)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_Reports_HasTarget",
            "\"ReportedUserId\" IS NOT NULL OR \"ItemId\" IS NOT NULL");

        builder.HasCheckConstraint(
            "CK_Reports_NotSelfReport",
            "\"ReportedUserId\" IS NULL OR \"ReporterId\" <> \"ReportedUserId\"");

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReportedUser)
            .WithMany()
            .HasForeignKey(x => x.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.Status,
            x.CreatedAt
        });

        builder.HasIndex(x => x.ReportedUserId);

        builder.HasIndex(x => x.ItemId);
    }
}
public class ItemImageConfiguration
    : IEntityTypeConfiguration<ItemImage>
{
    public void Configure(EntityTypeBuilder<ItemImage> builder)
    {
        builder.ToTable("ItemImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.S3ObjectKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.HasCheckConstraint(
            "CK_ItemImages_DisplayOrder_NonNegative",
            "\"DisplayOrder\" >= 0"
        );

        builder.HasOne(x => x.Item)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.ItemId,
            x.DisplayOrder
        })
        .IsUnique();
    }
}
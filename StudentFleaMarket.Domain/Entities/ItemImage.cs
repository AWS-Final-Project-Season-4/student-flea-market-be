namespace StudentFleaMarket.Domain.Entities;

public class ItemImage
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public string S3ObjectKey { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public Item Item { get; set; } = null!;
}

using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class Item
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }
    public Guid CategoryId { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public ItemCondition Condition { get; set; }

    public bool IsExchangeEnabled { get; set; }

    public ItemStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser Seller { get; set; } = null!;
    public Category Category { get; set; } = null!;

    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
}


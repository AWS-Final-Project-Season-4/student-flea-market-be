namespace StudentFleaMarket.Domain.Entities;

public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Condition { get; set; }
    public bool IsExchangeEnabled { get; set; } = false;
    public string Location { get; set; } = null!;
    public int Status { get; set; } = 1;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ApplicationUser Seller { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}



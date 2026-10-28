namespace StudentFleaMarket.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public Guid ItemId { get; set; }
    public decimal TotalAmount { get; set; }
    public int Method { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Navigation Properties
    public ApplicationUser Buyer { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public Payment? Payment { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}



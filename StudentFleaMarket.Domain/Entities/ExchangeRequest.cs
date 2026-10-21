namespace StudentFleaMarket.Domain.Entities;

public class ExchangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequesterId { get; set; }
    public Guid RequestedItemId { get; set; }
    public Guid OfferedItemId { get; set; }
    public decimal AdditionalAmount { get; set; } = 0;
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    // Navigation Properties
    public ApplicationUser Requester { get; set; } = null!;
    public Item RequestedItem { get; set; } = null!;
    public Item OfferedItem { get; set; } = null!;
}



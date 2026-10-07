namespace StudentFleaMarket.Domain.Entities;

public class ExchangeRequest
{
    public Guid Id { get; set; }

    public Guid RequesterId { get; set; }

    public Guid RequestedItemId { get; set; }

    public Guid? OfferedItemId { get; set; }

    public decimal AdditionalAmount { get; set; }

    public ExchangeRequestStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public ApplicationUser Requester { get; set; } = null!;

    public Item RequestedItem { get; set; } = null!;

    public Item? OfferedItem { get; set; }
}

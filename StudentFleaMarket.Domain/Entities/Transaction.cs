namespace StudentFleaMarket.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InitiatorId { get; set; }
    public Guid CounterpartyId { get; set; }
    public Guid ItemId { get; set; }
    public Guid? ExchangeRequestId { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public string MeetingLocation { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Navigation Properties
    public ApplicationUser Initiator { get; set; } = null!;
    public ApplicationUser Counterparty { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public ExchangeRequest? ExchangeRequest { get; set; }
    public Payment? Payment { get; set; }
    public Review? Review { get; set; }
}



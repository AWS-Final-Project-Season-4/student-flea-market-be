using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid InitiatorId { get; set; }

    public Guid CounterpartyId { get; set; }

    public Guid? ExchangeRequestId { get; set; }

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ApplicationUser Initiator { get; set; } = null!;

    public ApplicationUser Counterparty { get; set; } = null!;

    public ExchangeRequest? ExchangeRequest { get; set; }

    public ICollection<TransactionItem> Items { get; set; }
        = new List<TransactionItem>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();

    public ICollection<Review> Reviews { get; set; }
        = new List<Review>();
}


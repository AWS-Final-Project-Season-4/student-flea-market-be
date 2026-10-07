namespace StudentFleaMarket.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "VND";

    public string Provider { get; set; } = null!;

    public string ProviderTransactionId { get; set; } = null!;

    public PaymentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public Transaction Transaction { get; set; } = null!;
}

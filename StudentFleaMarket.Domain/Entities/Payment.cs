namespace StudentFleaMarket.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public int Amount { get; set; }
    public int Status { get; set; }
    public string PaymentUrl { get; set; } = null!;
    public string TransactionRef { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Transaction Transaction { get; set; } = null!;
}



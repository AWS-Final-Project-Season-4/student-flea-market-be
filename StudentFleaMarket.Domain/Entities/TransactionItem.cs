using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class TransactionItem
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public Guid ItemId { get; set; }

    public TransactionItemRole Role { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public Item Item { get; set; } = null!;
}


using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class Review
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public Guid ReviewerId { get; set; }

    public Guid RevieweeId { get; set; }

    public Guid ItemId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public ApplicationUser Reviewer { get; set; } = null!;

    public ApplicationUser Reviewee { get; set; } = null!;

    public Item Item { get; set; } = null!;
}


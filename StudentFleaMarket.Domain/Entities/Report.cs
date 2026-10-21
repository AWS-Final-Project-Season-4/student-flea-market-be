namespace StudentFleaMarket.Domain.Entities;

public class Report
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterId { get; set; }
    public Guid? ReportedUserId { get; set; }
    public Guid? ItemId { get; set; }
    public int Reason { get; set; }
    public string Description { get; set; } = null!;
    public int Status { get; set; }
    public string? AdminNote { get; set; }
    public Guid? ResolvedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // Navigation Properties
    public ApplicationUser Reporter { get; set; } = null!;
    public ApplicationUser? ReportedUser { get; set; }
    public Item? Item { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }
}



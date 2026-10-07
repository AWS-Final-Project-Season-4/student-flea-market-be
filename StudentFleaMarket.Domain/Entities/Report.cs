using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class Report
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }

    public Guid? ReportedUserId { get; set; }

    public Guid? ItemId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    public ReportStatus Status { get; set; }

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public ApplicationUser Reporter { get; set; } = null!;

    public ApplicationUser? ReportedUser { get; set; }

    public Item? Item { get; set; }
}


using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Domain.Entities;

public class Favorite
{
    public Guid UserId { get; set; }
    public Guid ItemId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Item Item { get; set; } = null!;
}


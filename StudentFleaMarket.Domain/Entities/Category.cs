namespace StudentFleaMarket.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}

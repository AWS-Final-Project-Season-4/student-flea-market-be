namespace StudentFleaMarket.Domain.Entities;

public class Category 
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public int Order { get; set; } = 0;

    //Navigation Properties (Single-level, NO ParentId)
    public ICollection<Item> Items { get; set; } = new List<Item>();
}


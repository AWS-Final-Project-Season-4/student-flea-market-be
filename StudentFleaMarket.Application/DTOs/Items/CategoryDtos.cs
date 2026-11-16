namespace StudentFleaMarket.Application.DTOs.Items;

public class CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string Slug { get; init; } = null!;
    public string? Description { get; init; }
    public int Order { get; init; }
    public int ItemCount { get; init; }
}

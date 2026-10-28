using Microsoft.AspNetCore.Identity;

namespace StudentFleaMarket.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? SchoolName { get; set; }
    public string? StudentCode { get; set; }
    
     /// <summary>
    /// 0: Student, 1: Admin
    /// </summary>
    public int Role { get; set; } = 0;

    /// <summary>
    /// 1: Active, 0: Inactive/Banned
    /// </summary>
    public int Status { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    //Navigation Properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}


namespace StudentFleaMarket.Domain.Entities;

public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PhoneNumber { get; set; } = null!;
    public string? AvatarUrl { get; set; } = null!;
    public string? SchoolName { get; set; } = null!;
    public string? StudentCode { get; set; } = null!; 
    
     /// <summary>
    /// 0: Student, 1: Admin
    /// </summary>
    public int Role { get; set; } = 0;

    /// <summary>
    /// 1: Active, 0: Inactive/Banned
    /// </summary>
    public int Status { get; set; } = 1;
    public bool IsEmailVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    //Navigation Properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}


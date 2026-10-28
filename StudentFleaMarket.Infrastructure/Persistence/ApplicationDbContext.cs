using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Domain.Entities;

namespace StudentFleaMarket.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemImage> ItemImages => Set<ItemImage>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ExchangeRequest> ExchangeRequests => Set<ExchangeRequest>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            entity.Property(x => x.SchoolName).HasMaxLength(200);
            entity.Property(x => x.StudentCode).HasMaxLength(50);
            entity.Property(x => x.AvatarUrl).HasMaxLength(500);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.EmailConfirmed).HasColumnName("IsEmailVerified");
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.Role).HasDefaultValue(0);
            entity.Property(x => x.Status).HasDefaultValue(1);
            entity.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
            entity.HasIndex(x => x.StudentCode).IsUnique().HasFilter("\"StudentCode\" IS NOT NULL");
        });

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.CategoryId, x.Status, x.CreatedAt });
            entity.HasIndex(x => new { x.SellerId, x.Status });
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(250);
            entity.Property(x => x.Description).HasMaxLength(5000);
            entity.Property(x => x.Price).HasPrecision(18, 2);
            entity.Property(x => x.Location).IsRequired().HasMaxLength(255);
            entity.HasIndex(x => x.SellerId);
            entity.HasIndex(x => x.CategoryId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.ToTable(t => t.HasCheckConstraint("CK_Items_Price_NonNegative", "\"Price\" >= 0"));
            entity.HasOne(x => x.Seller).WithMany(x => x.Items).HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany(x => x.Items).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ItemImage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
            entity.HasIndex(x => new { x.ItemId, x.Order }).IsUnique();
            entity.HasOne(x => x.Item).WithMany(x => x.Images).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.ItemId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.Favorites).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Item).WithMany(x => x.Favorites).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExchangeRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AdditionalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ExpiresAt);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_ExchangeRequests_AdditionalAmount", "\"AdditionalAmount\" IS NULL OR \"AdditionalAmount\" >= 0");
                t.HasCheckConstraint("CK_ExchangeRequests_DifferentItems", "\"OfferedItemId\" IS NULL OR \"OfferedItemId\" <> \"RequestedItemId\"");
            });
            entity.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RequestedItem).WithMany().HasForeignKey(x => x.RequestedItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.OfferedItem).WithMany().HasForeignKey(x => x.OfferedItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Transactions_DifferentUsers", "\"BuyerId\" <> \"SellerId\"");
                t.HasCheckConstraint("CK_Transactions_TotalAmount", "\"TotalAmount\" >= 0");
            });
            entity.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PaymentUrl).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.TransactionRef).IsRequired().HasMaxLength(150);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.TransactionRef);
            entity.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" >= 0"));
            entity.HasOne(x => x.Transaction).WithOne(x => x.Payment).HasForeignKey<Payment>(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.BuyerId, x.SellerId, x.ItemId }).IsUnique();
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired().HasMaxLength(5000);
            entity.HasIndex(x => new { x.ConversationId, x.SentAt });
            entity.HasIndex(x => x.SentAt);
            entity.HasIndex(x => x.IsRead);
            entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TransactionId, x.ReviewerId, x.RevieweeId }).IsUnique();
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Reviews_DifferentUsers", "\"ReviewerId\" <> \"RevieweeId\"");
            });
            entity.HasOne(x => x.Transaction).WithMany(x => x.Reviews).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reviewee).WithMany().HasForeignKey(x => x.RevieweeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.AdminNote).HasMaxLength(2000);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
            entity.ToTable(t => t.HasCheckConstraint("CK_Reports_HasTarget", "\"ReportedUserId\" IS NOT NULL OR \"ItemId\" IS NOT NULL"));
            entity.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReportedUser).WithMany().HasForeignKey(x => x.ReportedUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedBy).WithMany().HasForeignKey(x => x.ResolvedById).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

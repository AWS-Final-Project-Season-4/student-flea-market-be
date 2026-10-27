using Microsoft.EntityFrameworkCore;
using StudentFleaMarket.Domain.Entities;
using StudentFleaMarket.Domain.Enums;

namespace StudentFleaMarket.Infrastructure.Persistence;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Category> Category => Set<Category>();
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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();
    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        //ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity => 
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            // Username là định danh đăng nhập thay thế — chỉ unique khi có giá trị.
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Username).HasMaxLength(30);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            entity.Property(u => u.SchoolName).HasMaxLength(200);
            entity.Property(u => u.StudentCode).HasMaxLength(50);
            entity.Property(u => u.CommonArea).HasMaxLength(200);
            entity.Property(u => u.Dormitory).HasMaxLength(200);
            entity.Property(u => u.Role).HasDefaultValue((int)UserRole.User);
            entity.Property(u => u.Status).HasDefaultValue((int)UserStatus.Active);
            entity.Property(u => u.IsEmailVerified).HasDefaultValue(false);
        });

        //ServiceAccount (AI Service — xác thực bằng API key)
        modelBuilder.Entity<ServiceAccount>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.ApiKeyHash).IsUnique();
            entity.HasIndex(s => s.KeyPrefix);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.ApiKeyHash).IsRequired().HasMaxLength(128);
            entity.Property(s => s.KeyPrefix).IsRequired().HasMaxLength(16);
            entity.Property(s => s.Role).HasDefaultValue((int)UserRole.AiService);
            entity.Property(s => s.Status).HasDefaultValue((int)UserStatus.Active);
            entity.Property(s => s.RevokedReason).HasMaxLength(100);

            entity.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        //RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(t => t.RevokedReason).HasMaxLength(100);

            entity.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        //VerificationCode
        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.Email, c.CodeHash });
            entity.HasIndex(c => c.ExpiresAt);
            entity.Property(c => c.CodeHash).IsRequired().HasMaxLength(128);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(256);

            entity.HasOne(c => c.User)
                .WithMany(u => u.VerificationCodes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        //Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Order).HasDefaultValue(0); 
            
        });

        //Item
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.Slug).IsUnique();
            entity.Property(i => i.Title).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Slug).IsRequired().HasMaxLength(250);
            entity.Property(i => i.Price).HasPrecision(18, 2);
            entity.Property(i => i.Location).HasMaxLength(255);
            entity.Property(i => i.IsDeleted).HasDefaultValue(false);

            // Index hỗ trợ tìm kiếm & lọc nâng cao
            entity.HasIndex(i => i.Title);                       // tìm theo từ khóa tiêu đề
            entity.HasIndex(i => i.TitleNormalized);             // tìm theo từ khóa (đã bỏ dấu)
            entity.HasIndex(i => i.CategoryId);                 // lọc theo danh mục
            entity.HasIndex(i => i.Price);                      // lọc theo khoảng giá
            entity.HasIndex(i => i.Condition);                  // lọc theo độ mới
            entity.HasIndex(i => i.Status);                     // lọc theo trạng thái
            entity.HasIndex(i => i.SellerId);                   // lọc theo người bán
            entity.HasIndex(i => i.CreatedAt);                  // sắp xếp mới/cũ

            //Soft delete global filter
            entity.HasQueryFilter(i => !i.IsDeleted);

            //Seller relation (Restrict delete)
            entity.HasOne(i => i.Seller)
                .WithMany(u => u.Items)
                .HasForeignKey(i => i.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            //Category relation (Restrict delete)
            entity.HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
        });

        //ItemImage
        modelBuilder.Entity<ItemImage>(entity =>
        {
            entity.HasKey(img => img.Id);
            entity.Property(img => img.ImageUrl).IsRequired().HasMaxLength(500);

            //Relation
            entity.HasOne(img => img.Item)
                .WithMany(i => i.Images)
                .HasForeignKey(img => img.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
        });

        //Favorite (Composite unique index & Cascade on Item delete)
        modelBuilder.Entity<Favorite>(entity => 
        {
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => new { f.UserId, f.ItemId }).IsUnique();

           
            entity.HasOne(f => f.User)
                .WithMany(i => i.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(f => f.Item)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
        });

        //ExchangeRequest
        modelBuilder.Entity<ExchangeRequest>(entity =>
        {
            entity.HasKey(er => er.Id);
            entity.Property(er => er.AdditionalAmount).HasPrecision(18, 2);
            
            entity.HasOne(er => er.Requester)
                  .WithMany()
                  .HasForeignKey(er => er.RequesterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(er => er.RequestedItem)
                  .WithMany()
                  .HasForeignKey(er => er.RequestedItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        
            entity.HasOne(er => er.OfferedItem)
                  .WithMany()
                  .HasForeignKey(er => er.OfferedItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Transaction
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasPrecision(18,2);
            entity.Property(t => t.MeetingLocation).IsRequired().HasMaxLength(255);

            entity.HasOne(t => t.Initiator)
                  .WithMany()
                  .HasForeignKey(t => t.InitiatorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Counterparty)
                  .WithMany()
                  .HasForeignKey(t => t.CounterpartyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Item)
                  .WithMany()
                  .HasForeignKey(t => t.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(t => t.ExchangeRequest)
                  .WithMany()
                  .HasForeignKey(t => t.ExchangeRequestId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.TransactionRef).IsRequired().HasMaxLength(150);
            entity.HasOne(p => p.Transaction)
                  .WithOne(t => t.Payment)
                  .HasForeignKey<Payment>(p => p.TransactionId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(p => p.Payer)
                  .WithMany()
                  .HasForeignKey(p => p.PayerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Conversation
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasOne(c => c.Item)
                  .WithMany()
                  .HasForeignKey(c => c.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Buyer)
                  .WithMany()
                  .HasForeignKey(c => c.BuyerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Seller)
                  .WithMany()
                  .HasForeignKey(c => c.SellerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Message
         modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).IsRequired();
            entity.HasOne(m => m.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(m => m.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Sender)
                  .WithMany()
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Review (Unique on TransactionId)
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.TransactionId).IsUnique();
            entity.HasOne(r => r.Transaction)
                  .WithOne(t => t.Review)
                  .HasForeignKey<Review>(r => r.TransactionId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Reviewer)
                  .WithMany()
                  .HasForeignKey(r => r.ReviewerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Reviewee)
                  .WithMany()
                  .HasForeignKey(r => r.RevieweeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Report
        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(rep => rep.Id);
            entity.Property(rep => rep.Description).IsRequired();
            entity.HasOne(rep => rep.Reporter)
                  .WithMany()
                  .HasForeignKey(rep => rep.ReporterId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rep => rep.ReportedUser)
                  .WithMany()
                  .HasForeignKey(rep => rep.ReportedUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rep => rep.Item)
                  .WithMany()
                  .HasForeignKey(rep => rep.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rep => rep.ResolvedBy)
                  .WithMany()
                  .HasForeignKey(rep => rep.ResolvedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

    }

}
public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories
        => Set<Category>();

    public DbSet<Item> Items
        => Set<Item>();

    public DbSet<ItemImage> ItemImages
        => Set<ItemImage>();

    public DbSet<Favorite> Favorites
        => Set<Favorite>();

    public DbSet<Conversation> Conversations
        => Set<Conversation>();

    public DbSet<ConversationMember> ConversationMembers
        => Set<ConversationMember>();

    public DbSet<Message> Messages
        => Set<Message>();

    public DbSet<ExchangeRequest> ExchangeRequests
        => Set<ExchangeRequest>();

    public DbSet<Transaction> Transactions
        => Set<Transaction>();

    public DbSet<TransactionItem> TransactionItems
        => Set<TransactionItem>();

    public DbSet<Payment> Payments
        => Set<Payment>();

    public DbSet<Review> Reviews
        => Set<Review>();

    public DbSet<Report> Reports
        => Set<Report>();

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
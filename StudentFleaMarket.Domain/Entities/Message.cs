namespace StudentFleaMarket.Domain.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Conversation Conversation { get; set; } = null!;
    public ApplicationUser Buyer { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}



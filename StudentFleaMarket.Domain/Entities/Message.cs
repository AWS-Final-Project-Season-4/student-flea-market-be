namespace StudentFleaMarket.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}

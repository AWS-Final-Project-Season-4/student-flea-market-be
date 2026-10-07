namespace StudentFleaMarket.Domain.Entities;

public class ConversationMember
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }
    public DateTime? LastReadAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

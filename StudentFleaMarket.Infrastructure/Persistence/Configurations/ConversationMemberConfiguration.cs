public class ConversationMemberConfiguration
    : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.ToTable("ConversationMembers");

        builder.HasKey(x => new
        {
            x.ConversationId,
            x.UserId
        });

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
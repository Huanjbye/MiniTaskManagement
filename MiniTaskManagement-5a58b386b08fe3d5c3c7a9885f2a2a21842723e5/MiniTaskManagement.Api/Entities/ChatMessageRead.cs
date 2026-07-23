namespace MiniTaskManagement.Api.Entities;

public class ChatMessageRead
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public ChatMessage Message { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime ReadAt { get; set; }
}

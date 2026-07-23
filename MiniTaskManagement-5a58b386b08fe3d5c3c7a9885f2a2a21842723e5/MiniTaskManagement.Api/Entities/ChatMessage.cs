namespace MiniTaskManagement.Api.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;

    public Guid SenderId { get; set; }

    public User Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

    public string? AttachmentUrl { get; set; }

    public Guid? ReplyToMessageId { get; set; }

    public ChatMessage? ReplyToMessage { get; set; }

    public ICollection<ChatMessage> Replies { get; set; } =
        new List<ChatMessage>();

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<ChatMessageRead> Reads { get; set; } =
        new List<ChatMessageRead>();
}

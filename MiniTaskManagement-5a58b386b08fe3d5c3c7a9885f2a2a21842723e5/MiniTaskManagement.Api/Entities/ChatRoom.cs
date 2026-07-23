namespace MiniTaskManagement.Api.Entities;

public class ChatRoom
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public ChatRoomType Type { get; set; }

    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<ChatRoomMember> Members { get; set; } =
        new List<ChatRoomMember>();

    public ICollection<ChatMessage> Messages { get; set; } =
        new List<ChatMessage>();
}

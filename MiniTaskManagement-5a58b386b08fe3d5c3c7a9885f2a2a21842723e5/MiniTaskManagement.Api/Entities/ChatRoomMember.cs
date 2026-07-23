namespace MiniTaskManagement.Api.Entities;

public class ChatRoomMember
{
    public Guid Id { get; set; }

    public Guid ChatRoomId { get; set; }

    public ChatRoom ChatRoom { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public ChatRoomMemberRole Role { get; set; } = ChatRoomMemberRole.Member;

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public bool IsMuted { get; set; }

    public bool IsRemoved { get; set; }
}

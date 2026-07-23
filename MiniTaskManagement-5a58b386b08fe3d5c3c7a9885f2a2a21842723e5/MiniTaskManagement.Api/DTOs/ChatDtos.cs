using System.ComponentModel.DataAnnotations;
using MiniTaskManagement.Api.Entities;

namespace MiniTaskManagement.Api.DTOs;

public class ChatRoomDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public ChatRoomType Type { get; set; }

    public ChatMessageDto? LastMessage { get; set; }

    public int UnreadCount { get; set; }

    public List<ChatMemberDto> Members { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

public class ChatMemberDto
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public ChatRoomMemberRole Role { get; set; }

    public bool IsOnline { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid SenderId { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public ChatMessageType MessageType { get; set; }

    public string? AttachmentUrl { get; set; }

    public ChatMessageDto? ReplyToMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; }

    public List<ChatReadDto> ReadBy { get; set; } = [];
}

public class ChatReadDto
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime ReadAt { get; set; }
}

public class SendMessageRequest
{
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

    [MaxLength(1000)]
    public string? AttachmentUrl { get; set; }

    public Guid? ReplyToMessageId { get; set; }
}

public class CreateGroupChatRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public List<Guid> MemberIds { get; set; } = [];
}

public class AddMemberRequest
{
    [Required]
    public Guid UserId { get; set; }

    public ChatRoomMemberRole Role { get; set; } = ChatRoomMemberRole.Member;
}

public class UpdateRoomRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

public class ChatUserDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

namespace MiniTaskManagement.Api.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<TaskItem> Tasks { get; set; }
        = new List<TaskItem>();

    public ICollection<Project> Projects { get; set; }
        = new List<Project>();

    public ICollection<Tag> Tags { get; set; }
        = new List<Tag>();

    public ICollection<ChatRoomMember> ChatRoomMembers { get; set; }
        = new List<ChatRoomMember>();

    public ICollection<ChatMessage> ChatMessages { get; set; }
        = new List<ChatMessage>();
}

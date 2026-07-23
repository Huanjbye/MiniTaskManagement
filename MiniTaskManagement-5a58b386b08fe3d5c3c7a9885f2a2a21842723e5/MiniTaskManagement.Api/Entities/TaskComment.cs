namespace MiniTaskManagement.Api.Entities;

public class TaskComment
{
    public Guid Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Guid TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}

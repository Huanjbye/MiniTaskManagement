namespace MiniTaskManagement.Api.Entities;

public class TaskActivity
{
    public Guid Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}

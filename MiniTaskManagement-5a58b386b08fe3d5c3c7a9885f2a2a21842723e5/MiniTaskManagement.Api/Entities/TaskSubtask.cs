namespace MiniTaskManagement.Api.Entities;

public class TaskSubtask
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;
}

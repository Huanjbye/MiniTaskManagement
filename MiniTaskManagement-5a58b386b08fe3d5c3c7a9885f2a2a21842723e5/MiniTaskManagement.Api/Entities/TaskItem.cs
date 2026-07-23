namespace MiniTaskManagement.Api.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; } = string.Empty;

    public string Status { get; set; } = "Todo";

    public string Priority { get; set; } = "Medium";

    public DateTime DueDate { get; set; }

    public DateTime? ReminderAt { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int ActualMinutes { get; set; }

    public DateTime? TimerStartedAt { get; set; }

    public int SortOrder { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<TaskSubtask> Subtasks { get; set; } =
        new List<TaskSubtask>();

    public ICollection<TaskComment> Comments { get; set; } =
        new List<TaskComment>();

    public ICollection<TaskActivity> Activities { get; set; } =
        new List<TaskActivity>();

    public ICollection<TaskTag> TaskTags { get; set; } =
        new List<TaskTag>();
}

namespace MiniTaskManagement.Api.Entities;

public class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } =
        new List<TaskItem>();
}

namespace MiniTaskManagement.Api.Entities;

public class Tag
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#16b99f";

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public ICollection<TaskTag> TaskTags { get; set; } =
        new List<TaskTag>();
}

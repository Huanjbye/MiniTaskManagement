using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class CreateTaskRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string Priority { get; set; } = "Medium";

    [Required]
    public DateTime DueDate { get; set; }

    public Guid? ProjectId { get; set; }

    public DateTime? ReminderAt { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int ActualMinutes { get; set; }

    public List<string> TagNames { get; set; } = [];
}

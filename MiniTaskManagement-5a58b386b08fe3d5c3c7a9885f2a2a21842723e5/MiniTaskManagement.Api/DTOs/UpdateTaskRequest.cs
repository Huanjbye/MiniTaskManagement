using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class UpdateTaskRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;

    [Required]
    public DateTime DueDate { get; set; }

    public Guid? ProjectId { get; set; }

    public DateTime? ReminderAt { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int ActualMinutes { get; set; }

    public List<string> TagNames { get; set; } = [];
}

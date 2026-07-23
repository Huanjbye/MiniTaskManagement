using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class UpdateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string Status { get; set; } = "Active";

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }
}

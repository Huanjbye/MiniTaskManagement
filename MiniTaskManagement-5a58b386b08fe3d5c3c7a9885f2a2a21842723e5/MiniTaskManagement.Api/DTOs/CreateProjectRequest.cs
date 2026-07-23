using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class CreateProjectRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }
}

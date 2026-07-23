using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class UpsertSubtaskRequest
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public int SortOrder { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class UpdateTaskStatusRequest
{
    [Required]
    public string Status { get; set; } = "Todo";

    public int SortOrder { get; set; }
}

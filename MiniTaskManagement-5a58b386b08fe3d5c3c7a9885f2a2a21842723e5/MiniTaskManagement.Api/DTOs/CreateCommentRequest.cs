using System.ComponentModel.DataAnnotations;

namespace MiniTaskManagement.Api.DTOs;

public class CreateCommentRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}

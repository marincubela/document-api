using System.ComponentModel.DataAnnotations;

namespace Projekt.Dtos;

public class SendEmailRequest
{
    [Required]
    [EmailAddress]
    public string To { get; set; } = string.Empty;
    
    public string? Subject { get; set; }
    
    public string? Message { get; set; }
}


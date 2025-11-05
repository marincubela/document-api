using System.ComponentModel.DataAnnotations;

namespace Projekt.Dtos;

public class GrantAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}


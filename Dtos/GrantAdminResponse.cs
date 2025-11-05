namespace Projekt.Dtos;

public class GrantAdminResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<string> Roles { get; set; } = new();
}


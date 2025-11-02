namespace Projekt.Infrastructure.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public int RoleId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}


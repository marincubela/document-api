namespace Projekt.Infrastructure.Entities;

public class EmailLog
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid SenderUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "sent" or "failed"
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Document Document { get; set; } = null!;
    public User Sender { get; set; } = null!;
}


namespace Projekt.Infrastructure.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OriginalFilename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}


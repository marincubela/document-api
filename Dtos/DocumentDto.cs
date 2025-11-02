namespace Projekt.Dtos;

public class DocumentDto
{
    public Guid DocumentId { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid? OwnerUserId { get; set; }
}


namespace Projekt.Email;

public interface IEmailService
{
    /// <summary>
    /// Sends an email with an attachment
    /// </summary>
    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string body,
        Stream attachmentStream,
        string attachmentFileName,
        string attachmentContentType,
        CancellationToken cancellationToken = default);
}


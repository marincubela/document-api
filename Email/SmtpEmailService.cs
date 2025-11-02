using System.Net;
using System.Net.Mail;

namespace Projekt.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendEmailWithAttachmentAsync(
        string to,
        string subject,
        string body,
        Stream attachmentStream,
        string attachmentFileName,
        string attachmentContentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpHost = _configuration["Smtp:Host"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "25");
            var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "false");
            var smtpUser = _configuration["Smtp:User"];
            var smtpPassword = _configuration["Smtp:Password"];
            var fromAddress = _configuration["Smtp:From"] ?? "noreply@documentapi.local";

            using var message = new MailMessage();
            message.From = new MailAddress(fromAddress);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = false;

            // Add attachment
            // Create a copy of the stream in memory to avoid disposal issues
            var memoryStream = new MemoryStream();
            await attachmentStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            
            var attachment = new Attachment(memoryStream, attachmentFileName, attachmentContentType);
            message.Attachments.Add(attachment);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort);
            smtpClient.EnableSsl = enableSsl;

            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPassword))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            }

            await smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To}", to);

            // Generate a simple message ID
            var messageId = Guid.NewGuid().ToString();
            
            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return (false, null, ex.Message);
        }
    }
}


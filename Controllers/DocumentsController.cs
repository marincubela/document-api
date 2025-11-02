using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projekt.Dtos;
using Projekt.Infrastructure.Data;
using Projekt.Infrastructure.Entities;
using Projekt.Storage;
using Projekt.Email;

namespace Projekt.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IEmailService _emailService;
    private readonly ILogger<DocumentsController> _logger;
    private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB
    private static readonly string[] AllowedContentTypes = 
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public DocumentsController(
        ApplicationDbContext context,
        IFileStorage fileStorage,
        IEmailService emailService,
        ILogger<DocumentsController> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a document
    /// </summary>
    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        // Validate file exists
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, 
                new { error = $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024} MB" });
        }

        // Validate content type
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, 
                new { error = $"File type '{file.ContentType}' is not supported. Allowed types: PDF, DOCX" });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, 
                new { error = "File extension must be .pdf or .docx" });
        }

        try
        {
            var documentId = Guid.NewGuid();
            
            // Save file to storage
            string storageKey;
            await using (var stream = file.OpenReadStream())
            {
                storageKey = await _fileStorage.SaveFileAsync(stream, file.FileName, documentId);
            }

            // Get authenticated user ID
            var userId = GetAuthenticatedUserId();

            // Create document metadata
            var document = new Document
            {
                Id = documentId,
                OwnerUserId = userId,
                OriginalFilename = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                StorageKey = storageKey,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var dto = new DocumentDto
            {
                DocumentId = document.Id,
                Filename = document.OriginalFilename,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                UploadedAt = document.UploadedAt
            };

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while uploading the document" });
        }
    }

    /// <summary>
    /// Get document metadata or download document
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDocument(Guid id, [FromQuery] bool download = false)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound(new { error = "Document not found" });
        }

        // Check authorization
        if (!await CanAccessDocumentAsync(document.OwnerUserId))
        {
            return Forbid();
        }

        if (download)
        {
            try
            {
                var fileStream = await _fileStorage.GetFileAsync(document.StorageKey);
                return File(fileStream, document.ContentType, document.OriginalFilename);
            }
            catch (FileNotFoundException)
            {
                _logger.LogError("File not found in storage for document {DocumentId}", id);
                return NotFound(new { error = "Document file not found in storage" });
            }
        }

        var dto = new DocumentDto
        {
            DocumentId = document.Id,
            Filename = document.OriginalFilename,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            UploadedAt = document.UploadedAt,
            OwnerUserId = document.OwnerUserId
        };

        return Ok(dto);
    }

    /// <summary>
    /// Send document via email
    /// </summary>
    [Authorize]
    [HttpPost("{id}/send")]
    [ProducesResponseType(typeof(SendEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendDocument(Guid id, [FromBody] SendEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var document = await _context.Documents
            .Include(d => d.Owner)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound(new { error = "Document not found" });
        }

        // Check authorization
        if (!await CanAccessDocumentAsync(document.OwnerUserId))
        {
            return Forbid();
        }

        try
        {
            // Get file stream
            var fileStream = await _fileStorage.GetFileAsync(document.StorageKey);

            // Prepare email
            var subject = request.Subject ?? $"Document: {document.OriginalFilename}";
            var body = request.Message ?? $"Please find attached the document '{document.OriginalFilename}'.";

            // Send email
            var (success, messageId, errorMessage) = await _emailService.SendEmailWithAttachmentAsync(
                request.To,
                subject,
                body,
                fileStream,
                document.OriginalFilename,
                document.ContentType
            );

            // Log email attempt
            var emailLog = new EmailLog
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                SenderUserId = document.OwnerUserId,
                RecipientEmail = request.To,
                Status = success ? "sent" : "failed",
                ProviderMessageId = messageId,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync();

            if (success)
            {
                var response = new SendEmailResponse
                {
                    Status = "sent",
                    Recipient = request.To,
                    ProviderMessageId = messageId
                };

                return Ok(response);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to send email", details = errorMessage });
            }
        }
        catch (FileNotFoundException)
        {
            _logger.LogError("File not found in storage for document {DocumentId}", id);
            return NotFound(new { error = "Document file not found in storage" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document {DocumentId} via email", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while sending the email" });
        }
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("admin");
    }

    private Task<bool> CanAccessDocumentAsync(Guid documentOwnerId)
    {
        // Admins can access any document
        if (IsAdmin())
        {
            return Task.FromResult(true);
        }

        // Users can only access their own documents
        var userId = GetAuthenticatedUserId();
        return Task.FromResult(userId == documentOwnerId);
    }
}


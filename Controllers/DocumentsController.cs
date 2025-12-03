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

    private static readonly string[] AllowedContentTypes =
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public DocumentsController(
        ApplicationDbContext context,
        IFileStorage fileStorage,
        IEmailService emailService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _emailService = emailService;
    }

    /// <summary>
    /// Upload a document
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
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

    /// <summary>
    /// Get document metadata or download document
    /// </summary>
    [Authorize]
    [HttpGet("{id}")]
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
    public async Task<IActionResult> SendDocument(Guid id, [FromBody] SendEmailRequest request)
    {
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

        // Get file stream
        var fileStream = await _fileStorage.GetFileAsync(document.StorageKey);

        // Prepare email
        var subject = request.Subject ?? $"Document: {document.OriginalFilename}";
        var body = request.Message ?? $"Please find attached the document '{document.OriginalFilename}'.";

        // Send email
        var success = await _emailService.SendEmailWithAttachmentAsync(
            request.To,
            subject,
            body,
            fileStream,
            document.OriginalFilename,
            document.ContentType
        );

        if (!success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to send email" });
        }
        var response = new SendEmailResponse
        {
            Status = "sent",
            Recipient = request.To
        };

        return Ok(response);
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

    private Task<bool> CanAccessDocumentAsync(Guid documentOwnerId)
    {
        // Admins can access any document
        if (User.IsInRole("admin"))
        {
            return Task.FromResult(true);
        }

        // Users can only access their own documents
        var userId = GetAuthenticatedUserId();
        return Task.FromResult(userId == documentOwnerId);
    }
}
namespace Projekt.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:RootPath"] ?? "./storage";
        
        // Ensure root directory exists
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid documentId, CancellationToken cancellationToken = default)
    {
        // Sanitize filename
        var safeFileName = SanitizeFileName(fileName);
        
        // Create storage path: yyyy/MM/dd/{documentId}/{safeFileName}
        var now = DateTime.UtcNow;
        var relativePath = Path.Combine(
            now.Year.ToString(),
            now.Month.ToString("D2"),
            now.Day.ToString("D2"),
            documentId.ToString(),
            safeFileName
        );

        var fullPath = Path.Combine(_rootPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to temp file first, then move (atomic operation)
        var tempPath = fullPath + ".tmp";
        
        try
        {
            await using (var fileStreamOut = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await fileStream.CopyToAsync(fileStreamOut, cancellationToken);
            }

            // Move temp file to final location
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            // Clean up temp file if it exists
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }

        return relativePath;
    }

    public Task<Stream> GetFileAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found in storage", storageKey);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension.Substring(0, 200 - extension.Length) + extension;
        }

        return sanitized;
    }
}


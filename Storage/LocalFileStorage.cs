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
        // Create relative path. This will be used as a storage key as well.
        var relativePath = Path.Combine(documentId.ToString(), fileName);

        // Combine with root path to get full path
        var fullPath = Path.Combine(_rootPath, relativePath);
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write file to a given path.
        await using var fileStreamOut = new FileStream(fullPath, FileMode.Create);

        // We copy the sent stream (file) to the output stream (file on disk where we save it).
        await fileStream.CopyToAsync(fileStreamOut, cancellationToken);

        // Return the storage key.
        return relativePath;
    }

    public Task<Stream> GetFileAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found in storage", storageKey);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open);
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
}


namespace Projekt.Storage;

public interface IFileStorage
{
    /// <summary>
    /// Saves a file to storage and returns the storage key
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file from storage
    /// </summary>
    Task<Stream> GetFileAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    Task DeleteFileAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    Task<bool> FileExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}

